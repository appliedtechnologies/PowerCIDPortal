using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using Environment = at.D365.PowerCID.Portal.Data.Models.Environment;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class AsyncJobBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly IConfiguration configuration;
        private readonly GitHubService gitHubService;
        private readonly SolutionService solutionService;
        private readonly ActionService actionService;
        private readonly SolutionHistoryService solutionHistoryService;

        private System.Timers.Timer timer;

        public AsyncJobBackgroundService(IServiceProvider serviceProvider, ILogger<AsyncJobBackgroundService> logger)
        {
            this.serviceProvider = serviceProvider;

            var scope = serviceProvider.CreateScope();

            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.gitHubService = scope.ServiceProvider.GetRequiredService<GitHubService>();
            this.solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
            this.actionService = scope.ServiceProvider.GetRequiredService<ActionService>();
            this.solutionHistoryService = scope.ServiceProvider.GetRequiredService<SolutionHistoryService>();
            this.logger = logger;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ScheduleJob(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Stop();
            logger.LogInformation("Jobservice completed in {0}");
            await Task.CompletedTask;
        }

        protected async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = DateTimeOffset.Now.AddSeconds(int.Parse(configuration["AsyncJobIntervalSeconds"]));
            var delay = next - DateTimeOffset.Now;
            if (delay.TotalMilliseconds <= 0) // prevent non-positive values from being passed into Timer
            {
                await ScheduleJob(cancellationToken);
            }
            timer = new System.Timers.Timer(delay.TotalMilliseconds);
            timer.Elapsed += async (sender, args) =>
            {
                timer.Dispose(); // reset and dispose timer
                timer = null;

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await DoBackgroundWork(cancellationToken);
                    }
                    catch (System.Exception e)
                    {
                        logger.LogWarning(e, "LogWarning: Error while processing the Task ScheduleJob");
                    }

                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleJob(cancellationToken); // reschedule next
                }
            };
            timer.Start();
            await Task.CompletedTask;
        }

        private async Task DoBackgroundWork(CancellationToken cancellationToken)
        {

            var asyncJobsByEnvironment = dbContext.AsyncJobs.ToList().GroupBy(e => e.ActionNavigation.TargetEnvironment);

            if (asyncJobsByEnvironment.Count() > 0)
            {
                foreach (var environmentGroup in asyncJobsByEnvironment)
                {
                    try{
                        var environmentId = environmentGroup.Key;
                        Environment environment = dbContext.Environments.Find(environmentId);

                        foreach (AsyncJob asyncJob in environmentGroup)
                        {
                            try{
                                switch(asyncJob.ActionNavigation.Type){
                                    case 1: //export
                                    {
                                        Entity asyncOperationInDataverse = await this.GetCurrentAsyncOperationFromDataverse((Guid)asyncJob.AsyncOperationId, environment.BasicUrl);

                                        if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 30){ // Completed Success
                                            string exportSolutionFile = await this.solutionService.DownloadSolutionFileFromDataverse(asyncJob);
                                            await this.gitHubService.SaveSolutionFile(asyncJob, exportSolutionFile, environment.TenantNavigation);
                                            this.actionService.UpdateSuccessfulAction(asyncJob.ActionNavigation);

                                            // Export with Import --> Start Import
                                            if (asyncJob.ActionNavigation.ExportOnly == false && asyncJob.IsManaged == true)
                                            {
                                                try
                                                {
                                                    await this.solutionService.AddImportAction((int)asyncJob.ActionNavigation.Solution, (int)asyncJob.ActionNavigation.ImportTargetEnvironment, asyncJob.ActionNavigation.CreatedByNavigation.MsId);
                                                }
                                                catch (Exception e)
                                                {
                                                    dbContext.Actions.Add(new Data.Models.Action
                                                    {
                                                        Name = $"{asyncJob.ActionNavigation.SolutionNavigation.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                                                        TargetEnvironment = (int)asyncJob.ActionNavigation.ImportTargetEnvironment,
                                                        Type = 2,
                                                        Status = 3,
                                                        Result = 2,
                                                        ErrorMessage = e.Message,
                                                        StartTime = DateTime.Now,
                                                        Solution = asyncJob.ActionNavigation.Solution,
                                                    });
                                                }
                                            }
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                        else if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 31) // Completed Failed
                                        {
                                            await this.actionService.UpdateFailedAction(asyncJob.ActionNavigation, (string)asyncOperationInDataverse["friendlymessage"], asyncJob);
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                    }
                                    break;
                                    case 2: //import
                                    {
                                        Entity asyncOperationInDataverse = await this.GetCurrentAsyncOperationFromDataverse((Guid)asyncJob.AsyncOperationId, environment.BasicUrl);

                                        if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 30){ // Completed Success
                                            this.actionService.UpdateSuccessfulAction(asyncJob.ActionNavigation);

                                            // Upgrade Solution without manuelly upgrade apply --> Start Apply Upgrade
                                            bool isPatch = asyncJob.ActionNavigation.SolutionNavigation.GetType().Name.Contains("Patch");
                                            if (!isPatch)
                                            {
                                                Upgrade upgrade = (Upgrade)asyncJob.ActionNavigation.SolutionNavigation;
                                                bool existsSolutionInTargetEnvironment = await this.solutionService.ExistsSolutionInTargetEnvironment(upgrade.UniqueName, asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl, upgrade.Version);
                                                if (existsSolutionInTargetEnvironment == true && upgrade.ApplyManually == false && asyncJob.IsManaged == true && asyncJob.ActionNavigation.Status != 4)
                                                {
                                                    try
                                                    {
                                                        await this.solutionService.AddApplyUpgradeAction((int)asyncJob.ActionNavigation.Solution, (int)asyncJob.ActionNavigation.TargetEnvironment, asyncJob.ActionNavigation.CreatedByNavigation.MsId);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        dbContext.Actions.Add(new Data.Models.Action
                                                        {
                                                            Name = $"{asyncJob.ActionNavigation.SolutionNavigation.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                                                            TargetEnvironment = (int)asyncJob.ActionNavigation.ImportTargetEnvironment,
                                                            Type = 3,
                                                            Status = 3,
                                                            Result = 2,
                                                            ErrorMessage = e.Message,
                                                            StartTime = DateTime.Now,
                                                            Solution = asyncJob.ActionNavigation.Solution,
                                                        });
                                                    }
                                                }
                                            }
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                        else if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 31) // Completed Failed
                                        {
                                            await this.actionService.UpdateFailedAction(asyncJob.ActionNavigation, (string)asyncOperationInDataverse["friendlymessage"], asyncJob);
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                    }
                                    break;
                                    case 3: //appling upgrade
                                    {
                                        Entity solutionHistoryEntry = await this.solutionHistoryService.GetEntryById((Guid)asyncJob.JobId, environment.BasicUrl);

                                        if (solutionHistoryEntry["msdyn_endtime"] != null && ((OptionSetValue)solutionHistoryEntry["msdyn_status"]).Value == 1)
                                        {
                                            if ((bool)solutionHistoryEntry["msdyn_result"] == false)
                                                await this.actionService.UpdateFailedAction(asyncJob.ActionNavigation, (string)solutionHistoryEntry["msdyn_exceptionmessage"]);
                                            else
                                                this.actionService.UpdateSuccessfulAction(asyncJob.ActionNavigation);

                                            dbContext.Remove(asyncJob);
                                        }
                                    }
                                    break;
                                    default:
                                        throw new Exception($"Unknow ActionType for AsyncJob: {asyncJob.ActionNavigation.Type}");
                                }
                                await dbContext.SaveChangesAsync(asyncJob.ActionNavigation.CreatedByNavigation.MsId);                              
                            }
                            catch(Exception e){
                                logger.LogError(e, "error while processing AsyncJob (foreach AsyncJob )");
                                continue;
                            }
                        }
                    }
                    catch(Exception e){
                        logger.LogError(e, "error while processing AsyncJob (foreach environmentGroup)");
                        continue;
                    }
                }
            }
        }

        private async Task<Entity> GetCurrentAsyncOperationFromDataverse(Guid asyncOperationId, string basicUrl){
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                Entity response = await dataverseClient.RetrieveAsync("asyncoperation", asyncOperationId, new ColumnSet("asyncoperationid", "statecode", "statuscode", "friendlymessage"));
                return response;
            }
        }
    }
}