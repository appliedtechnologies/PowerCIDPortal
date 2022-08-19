using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
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
        private readonly IConfiguration configuration;
        private readonly SolutionService solutionService;
        private readonly atPowerCIDContext dbContext;
        private readonly GitHubService gitHubService;
        private readonly SolutionHistoryService solutionHistoryService;
        private readonly ActionService actionService;
        private System.Timers.Timer timer;

        public ActionBackgroundService(IServiceProvider serviceProvider, ILogger<ActionBackgroundService> logger)
        {
            var scope = serviceProvider.CreateScope();

            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
            this.gitHubService = scope.ServiceProvider.GetRequiredService<GitHubService>();
            this.solutionHistoryService = scope.ServiceProvider.GetRequiredService<SolutionHistoryService>();
            this.actionService = scope.ServiceProvider.GetRequiredService<ActionService>();
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

            var next = DateTimeOffset.Now.AddSeconds(int.Parse(configuration["ActionBackgroundJobIntervalSeconds"]));
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

            foreach (Action queuedAction in this.dbContext.Actions.Where(e => e.Status == 1).ToList())
            {
                try{
                    switch(queuedAction.Type){
                        case 1: //export
                        {
                            logger.LogInformation("Export started: ActionBackgroundService DoBackgroundWork()");

                            var asyncJobManaged = await this.solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, true, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);
                            var asyncJobUnmanaged = await this.solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, false, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);

                            dbContext.Add(asyncJobManaged);
                            dbContext.Add(asyncJobUnmanaged);

                            queuedAction.Status = 2;
                            await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);                          

                            logger.LogInformation("Export completed: ActionBackgroundService DoBackgroundWork()");
                        }
                        break;
                        case 2: //import 
                        {
                            logger.LogInformation("Import started: ActionBackgroundService DoBackgroundWork()");

                            byte[] exportSolutionFile = await this.gitHubService.GetSolutionFileAsByteArray(queuedAction.TargetEnvironmentNavigation.TenantNavigation, queuedAction.SolutionNavigation);
                            AsyncJob asyncJobManaged = await this.solutionService.StartImportInDataverse(exportSolutionFile, queuedAction);

                            dbContext.Add(asyncJobManaged);

                            queuedAction.Status = 2;
                            await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                            logger.LogInformation("Imported completed: ActionBackgroundService DoBackgroundWork()");
                        }
                        break;
                        case 3:  //appling upgrade
                        {
                            logger.LogInformation("Appling upgrade started: ActionBackgroundService DoBackgroundWork()");

                            queuedAction.Status = 2;
                            await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                            AsyncJob asyncJob = await this.solutionService.DeleteAndPromoteInDataverse(queuedAction);

                            if(asyncJob == null)
                                this.actionService.UpdateSuccessfulAction(queuedAction);
                            else
                                dbContext.Add(asyncJob);
                                
                            await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                            logger.LogInformation("Appling upgrade completed: ActionBackgroundService DoBackgroundWork()");
                        }  
                        break;
                        default: 
                            throw new Exception($"unknow ActionType: {queuedAction.Type}");
                    }             
                }
                catch(Exception e){
                    await this.actionService.UpdateFailedAction(queuedAction, e.Message);
                    await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);

                    logger.LogError($"Error: ActionBackgroundService DoBackgroundWork() Exception: {e}");
                    
                    continue;
                }
            }
            logger.LogDebug("End: ActionBackgroundService DoBackgroundWork()");
        }
    }
}