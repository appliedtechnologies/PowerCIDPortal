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
            logger.LogDebug("Begin: AsyncJobBackgroundService Dispose()");

            timer?.Dispose();

            logger.LogDebug("End: AsyncJobBackgroundService Dispose()");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: AsyncJobBackgroundService StartAsync()");

            if(!bool.Parse(configuration["BackgroundServices:DisableBackgroundServices"]))
                await ScheduleJob(cancellationToken);

            logger.LogDebug("End: AsyncJobBackgroundService StartAsync()");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: AsyncJobBackgroundService StopAsync()");

            timer?.Stop();
            await Task.CompletedTask;

            logger.LogDebug("End: AsyncJobBackgroundService StopAsync()");
        }

        protected async Task ScheduleJob(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: AsyncJobBackgroundService ScheduleJob()");

            var next = DateTimeOffset.Now.AddSeconds(int.Parse(configuration["BackgroundServices:AsyncJobIntervalSeconds"]));
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
                        logger.LogError($"Error: AsyncJobBackgroundService ScheduleJob() Exception: {e}");
                    }

                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleJob(cancellationToken); // reschedule next
                }
            };
            timer.Start();
            await Task.CompletedTask;

            logger.LogDebug("End: AsyncJobBackgroundService ScheduleJob()");
        }

        private async Task DoBackgroundWork(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: AsyncJobBackgroundService DoBackgroundWork()");

            var asyncJobsByEnvironment = dbContext.AsyncJobs.ToList().GroupBy(e => e.ActionNavigation.TargetEnvironment);

            if (asyncJobsByEnvironment.Count() > 0)
            {
                foreach (var environmentGroup in asyncJobsByEnvironment)
                {
                    try
                    {
                        var environmentId = environmentGroup.Key;
                        Environment environment = dbContext.Environments.Find(environmentId);

                        foreach (AsyncJob asyncJob in environmentGroup)
                        {
                            try
                            {
                                switch (asyncJob.ActionNavigation.Type)
                                {
                                    case 1: //export
                                        {
                                            logger.LogInformation("Export started: AsyncJobBackgroundService DoBackgroundWork() ");

                                            Entity asyncOperationInDataverse = await this.GetCurrentAsyncOperationFromDataverse((Guid)asyncJob.AsyncOperationId, environment.BasicUrl);

                                            if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 30)
                                            { // Completed Success
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
                                            logger.LogInformation("Export completed: AsyncJobBackgroundService DoBackgroundWork() ");
                                        }
                                        break;
                                    case 2: //import
                                        {
                                            logger.LogInformation("Import started: AsyncJobBackgroundService DoBackgroundWork() ");

                                            Entity asyncOperationInDataverse = await this.GetCurrentAsyncOperationFromDataverse((Guid)asyncJob.AsyncOperationId, environment.BasicUrl);

                                            if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 30)
                                            { // Completed Success
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
                                            logger.LogInformation("Import completed: AsyncJobBackgroundService DoBackgroundWork() ");
                                        }
                                        break;
                                    case 3: //appling upgrade
                                        {
                                            logger.LogInformation("Appling upgrade started: AsyncJobBackgroundService DoBackgroundWork() ");

                                            Entity solutionHistoryEntry = await this.solutionHistoryService.GetEntryById((Guid)asyncJob.JobId, environment.BasicUrl);

                                            if (solutionHistoryEntry["msdyn_endtime"] != null && ((OptionSetValue)solutionHistoryEntry["msdyn_status"]).Value == 1)
                                            {
                                                if ((bool)solutionHistoryEntry["msdyn_result"] == false)
                                                    await this.actionService.UpdateFailedAction(asyncJob.ActionNavigation, (string)solutionHistoryEntry["msdyn_exceptionmessage"]);
                                                else
                                                    await this.actionService.FinishSuccessfulApplyUpgradeAction(asyncJob.ActionNavigation);
                                                
                                                dbContext.Remove(asyncJob);
                                            }
                                          logger.LogInformation("Appling upgrade completed: AsyncJobBackgroundService DoBackgroundWork()");
                                        }
                                        break;
                                    default:
                                        throw new Exception($"Unknow ActionType for AsyncJob: {asyncJob.ActionNavigation.Type}");
                                }
                                await dbContext.SaveChangesAsync(asyncJob.ActionNavigation.CreatedByNavigation.MsId);
                            }
                            catch (Exception e)
                            {
                                logger.LogError($"Error: AsyncJobBackgroundService DoBackgroundWork() Exception: {e}");

                                continue;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Error: AsyncJobBackgroundService DoBackgroundWork() Exception: {e}");

                        continue;
                    }
                }
            }
            logger.LogDebug($"End: AsyncJobBackgroundService DoBackgroundWork()");
        }

        private async Task<Entity> GetCurrentAsyncOperationFromDataverse(Guid asyncOperationId, string basicUrl)
        {
            logger.LogDebug($"Begin: AsyncJobBackgroundService GetCurrentAsyncOperationFromDataverse(asyncOperationId: {asyncOperationId.ToString()}, basicUrl: {basicUrl})");

            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                Entity response = await dataverseClient.RetrieveAsync("asyncoperation", asyncOperationId, new ColumnSet("asyncoperationid", "statecode", "statuscode", "friendlymessage"));

                logger.LogDebug($"End: AsyncJobBackgroundService GetCurrentAsyncOperationFromDataverse(asyncOperationId: {asyncOperationId.ToString()}, basicUrl: {basicUrl})");

                return response;
            }
        }
    }
}