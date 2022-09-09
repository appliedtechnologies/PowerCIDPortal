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
        private readonly SolutionService solutionService;
        private readonly GitHubService gitHubService;
        private readonly SolutionHistoryService solutionHistoryService;
        private readonly ActionService actionService;
        private readonly FlowService flowService;
        private System.Timers.Timer timer;

        public ActionBackgroundService(IServiceProvider serviceProvider, ILogger<ActionBackgroundService> logger)
        {
            this.serviceProvider = serviceProvider;
            var scope = this.serviceProvider.CreateScope();

            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
            this.gitHubService = scope.ServiceProvider.GetRequiredService<GitHubService>();
            this.solutionHistoryService = scope.ServiceProvider.GetRequiredService<SolutionHistoryService>();
            this.actionService = scope.ServiceProvider.GetRequiredService<ActionService>();
            this.flowService = scope.ServiceProvider.GetRequiredService<FlowService>();
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

            using(var dbContext = this.serviceProvider.CreateScope().ServiceProvider.GetRequiredService<atPowerCIDContext>()){
                foreach (Action queuedAction in dbContext.Actions.Where(e => e.Status == 1).ToList())
                {
                    try
                    {
                        switch (queuedAction.Type)
                        {
                            case 1: //export
                            {
                                var asyncJobManaged = await this.solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, true, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);
                                var asyncJobUnmanaged = await this.solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, false, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);

                                dbContext.Add(asyncJobManaged);
                                dbContext.Add(asyncJobUnmanaged);

                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                            }
                            break;
                            case 2: //import 
                            {                          
                                byte[] exportSolutionFile = await this.gitHubService.GetSolutionFileAsByteArray(queuedAction.TargetEnvironmentNavigation.TenantNavigation, queuedAction.SolutionNavigation, queuedAction.TargetEnvironmentNavigation.DeployUnmanaged);
                                AsyncJob asyncJob = await this.solutionService.StartImportInDataverse(exportSolutionFile, queuedAction);

                                dbContext.Add(asyncJob);

                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId); 
                            }
                            break;
                            case 3: //apply upgrade
                            {                        
                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                                AsyncJob asyncJob = await this.solutionService.DeleteAndPromoteInDataverse(queuedAction);

                                if(asyncJob == null)
                                    await this.actionService.FinishSuccessfulApplyUpgradeAction(queuedAction);
                                else
                                    dbContext.Add(asyncJob);
                                    
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                            }  
                            break;
                            case 4: //enable flows
                            {
                                queuedAction.Status = 2;
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                                string errorLog = await this.flowService.EnableAllCloudFlows(queuedAction.SolutionNavigation.UniqueName, queuedAction.TargetEnvironmentNavigation.ConnectionsOwner, queuedAction.TargetEnvironmentNavigation.BasicUrl, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl);

                                if(String.IsNullOrEmpty(errorLog))
                                    this.actionService.UpdateSuccessfulAction(queuedAction);
                                else
                                    await this.actionService.UpdateFailedAction(queuedAction, errorLog);
                                    
                                await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                            }  
                            break;
                            default: 
                                throw new Exception($"unknow ActionType: {queuedAction.Type}");
                        }
                    }
                    catch (Exception e)
                    {
                        await this.actionService.UpdateFailedAction(queuedAction, e.Message);
                        await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                        logger.LogError($"Error: ActionBackgroundService DoBackgroundWork() Exception: {e}");

                        continue;
                    }
                }
            }
            logger.LogDebug("End: ActionBackgroundService DoBackgroundWork()");
        }
    }
}