using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Action = at.D365.PowerCID.Portal.Data.Models.Action;

namespace at.D365.PowerCID.Portal.Services
{
    public class ActionBackgroundService : IHostedService, IDisposable
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        private System.Timers.Timer timer;

        public ActionBackgroundService(IServiceProvider serviceProvider, ILogger<ActionBackgroundService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.configuration = serviceProvider.GetRequiredService<IConfiguration>();
            this.logger = logger;
        }

        public void Dispose()
        {
            logger.LogDebug("Begin: ActionBackgroundService Dispose()");

            timer?.Dispose();

            logger.LogDebug("End: ActionBackgroundService Dispose()");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: ActionBackgroundService StartAsync()");

            if(!bool.Parse(configuration["BackgroundServices:DisableBackgroundServices"]))
                await ScheduleBackgroundJob(cancellationToken);

            logger.LogDebug("End: ActionBackgroundService StartAsync()");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: ActionBackgroundService StopAsync()");

            timer?.Stop();
            await Task.CompletedTask;

            logger.LogDebug("End: ActionBackgroundService StopAsync()");
        }

        private async Task ScheduleBackgroundJob(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: ActionBackgroundService ScheduleBackgroundJob()");

            var next = DateTimeOffset.Now.AddSeconds(int.Parse(configuration["BackgroundServices:ActionBackgroundJobIntervalSeconds"]));
            var delay = next - DateTimeOffset.Now;
            if (delay.TotalMilliseconds <= 0) // prevent non-positive values from being passed into Timer
            {
                await ScheduleBackgroundJob(cancellationToken);
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
                        logger.LogError($"Error: ActionBackgroundService ScheduleBackgroundJob() Exception: {e}");
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleBackgroundJob(cancellationToken); // reschedule next
                }
            };
            timer.Start();
            await Task.CompletedTask;

            logger.LogDebug("End: ActionBackgroundService ScheduleBackgroundJob()");
        }

        private async Task DoBackgroundWork(CancellationToken cancellationToken)
        {
            logger.LogDebug("Begin: ActionBackgroundService DoBackgroundWork()");

            using(var scope = this.serviceProvider.CreateScope()){
                var dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
                var solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
                var gitHubService = scope.ServiceProvider.GetRequiredService<GitHubService>();
                var solutionHistoryService = scope.ServiceProvider.GetRequiredService<SolutionHistoryService>();
                var actionService = scope.ServiceProvider.GetRequiredService<ActionService>();
                var flowService = scope.ServiceProvider.GetRequiredService<FlowService>();

                foreach (Action queuedAction in dbContext.Actions.Where(e => e.Status == 1).ToList())
                {
                    logger.LogDebug($"queuedAction Id: {queuedAction.Id}");
                    try
                    {
                        switch (queuedAction.Type)
                        {
                            case 1: //export
                            {
                                var asyncJobManaged = await solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, true, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);
                                var asyncJobUnmanaged = await solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, false, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);

                                dbContext.Add(asyncJobManaged);
                                dbContext.Add(asyncJobUnmanaged);

                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                            }
                            break;
                            case 2: //import 
                            {                          
                                byte[] exportSolutionFile = await gitHubService.GetSolutionFileAsByteArray(queuedAction.TargetEnvironmentNavigation.TenantNavigation, queuedAction.SolutionNavigation, queuedAction.TargetEnvironmentNavigation.DeployUnmanaged);
                                AsyncJob asyncJob;

                                Solution solution = dbContext.Solutions.FirstOrDefault(s => s.Id == queuedAction.Solution);
                                bool isPatch = solution.GetType().Name.Contains("Patch");
                                bool existsSolutionInTargetEnvironment = await solutionService.ExistsSolutionInTargetEnvironment(queuedAction.SolutionNavigation.UniqueName, queuedAction.TargetEnvironmentNavigation.BasicUrl);
                                var holdingSolution = !queuedAction.TargetEnvironmentNavigation.DeployUnmanaged && !isPatch && existsSolutionInTargetEnvironment && ((Upgrade)solution).ApplyManually;

                                if (holdingSolution) //as holding upgrade
                                    asyncJob = await solutionService.StartImportInDataverse(exportSolutionFile, queuedAction, true);
                                else if (isPatch || !existsSolutionInTargetEnvironment) //as patch/upgrade
                                    asyncJob = await solutionService.StartImportInDataverse(exportSolutionFile, queuedAction, false);
                                else //as direct upgrade
                                    asyncJob = await solutionService.StartImportAndUpgradeInDataverse(exportSolutionFile, queuedAction);                                

                                dbContext.Add(asyncJob);

                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId); 
                            }
                            break;
                            case 3: //apply upgrade
                            {                        
                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                                AsyncJob asyncJob = await solutionService.DeleteAndPromoteInDataverse(queuedAction);

                                if(asyncJob == null)
                                    await actionService.FinishSuccessfulApplyUpgradeAction(queuedAction);
                                else
                                    dbContext.Add(asyncJob);
                                    
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                            }  
                            break;
                            case 4: //enable flows
                            {
                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                                string errorLog = await flowService.EnableAllCloudFlows(queuedAction.SolutionNavigation.UniqueName, queuedAction.TargetEnvironmentNavigation.ConnectionsOwner, queuedAction.TargetEnvironmentNavigation.BasicUrl, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl);

                                if(String.IsNullOrEmpty(errorLog))
                                    actionService.UpdateSuccessfulAction(queuedAction);
                                else
                                    await actionService.UpdateFailedAction(queuedAction, errorLog);
                                    
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                            }  
                            break;
                            default: 
                                throw new Exception($"unknow ActionType: {queuedAction.Type}");
                        }
                    }
                    catch (Exception e)
                    {
                        await actionService.UpdateFailedAction(queuedAction, e.Message, queuedAction.AsyncJobs.FirstOrDefault());
                        await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                        logger.LogError(e, $"Error: ActionBackgroundService DoBackgroundWork() Exception: {e}");

                        continue;
                    }
                }
            }
            logger.LogDebug("End: ActionBackgroundService DoBackgroundWork()");
        }
    }
}