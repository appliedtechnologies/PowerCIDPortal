using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Action = at.D365.PowerCID.Portal.Data.Models.Action;

namespace at.D365.PowerCID.Portal.Services
{
    public class ActionService : IHostedService, IDisposable
    {
        private readonly IConfiguration configuration;
        private readonly SolutionService solutionService;
        private readonly atPowerCIDContext dbContext;
        private System.Timers.Timer timer;

        public ActionService(IServiceProvider serviceProvider)
        {
            var scope = serviceProvider.CreateScope();

            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ScheduleBackgroundJob(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Stop();
            await Task.CompletedTask;
        }

        private async Task ScheduleBackgroundJob(CancellationToken cancellationToken)
        {
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
                    catch (System.Exception)
                    {
                        // TODO Logging;
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleBackgroundJob(cancellationToken); // reschedule next
                }
            };
            timer.Start();
            await Task.CompletedTask; 
        }

        private async Task DoBackgroundWork(CancellationToken cancellationToken)
        {
            foreach (Action queuedAction in this.dbContext.Actions.Where(e => e.Status == 1).ToList())
            {
                try{
                    if(queuedAction.Type == 1) //export
                    {
                        var asyncJobManaged = await this.solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, true, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);
                        var asyncJobUnmanaged = await this.solutionService.StartExportInDataverse(queuedAction.SolutionNavigation.UniqueName, false, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, queuedAction, queuedAction.CreatedByNavigation.TenantNavigation.MsId, queuedAction.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironment, queuedAction.ImportTargetEnvironment ?? 0);

                        asyncJobManaged.ActionNavigation = queuedAction;
                        asyncJobUnmanaged.ActionNavigation = queuedAction;

                        dbContext.Add(asyncJobManaged);
                        dbContext.Add(asyncJobUnmanaged);
                        queuedAction.Status = 2;
                        await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                    }
                    else if(queuedAction.Type == 2) //import 
                    {
                        byte[] exportSolutionFile = await this.solutionService.GetSolutionFromGitHub(queuedAction.TargetEnvironmentNavigation.TenantNavigation, queuedAction.SolutionNavigation);
                        AsyncJob asyncJobManaged = await this.solutionService.StartImportInDataverse(exportSolutionFile, queuedAction);

                        asyncJobManaged.ActionNavigation = queuedAction;

                        dbContext.Add(asyncJobManaged);
                        queuedAction.Status = 2;
                        await dbContext.SaveChangesAsync(msIdCurrentUser: queuedAction.CreatedByNavigation.MsId);
                    }
                }
                catch(Exception e){
                    //TODO logging
                    continue;
                }
            }
        }
    }
}