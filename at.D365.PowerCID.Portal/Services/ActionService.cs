using System;
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
    public class ActionService
    {
        private readonly ILogger<ActionService> logger;
        private readonly IConfiguration configuration;
        private readonly SolutionService solutionService;
        private readonly atPowerCIDContext dbContext;
        private readonly SolutionHistoryService solutionHistoryService;

        public ActionService(IServiceProvider serviceProvider, ILogger<ActionService> logger)
        {
            var scope = serviceProvider.CreateScope();

            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
            this.solutionHistoryService = scope.ServiceProvider.GetRequiredService<SolutionHistoryService>();
            this.logger = logger;
        }

        public void UpdateSuccessfulAction(Action action)
        {
            logger.LogDebug($"Begin: ActionService UpdateSuccessfulAction()");

            action.Status = 3;
            action.Result = 1;
            action.FinishTime = DateTime.Now;

            logger.LogDebug($"End: ActionService UpdateSuccessfulAction()");
        }

        public async Task UpdateFailedAction(Action action, string friendlyErrormessage, AsyncJob asyncJobForExeptionMessage = null)
        {
            logger.LogDebug($"Begin: ActionService UpdateFailedAction(friendlyErrormessage: {friendlyErrormessage})");

            action.Status = 3;
            action.Result = 2;
            action.FinishTime = DateTime.Now;

            action.ErrorMessage = friendlyErrormessage;
            if ((action.ErrorMessage == String.Empty || action.ErrorMessage == "An unexpected error occurred.")&& asyncJobForExeptionMessage != null)
            {
                var asyncJobExeptionMessage = await this.solutionHistoryService.GetExceptionMessage(asyncJobForExeptionMessage);
                if(!String.IsNullOrEmpty(asyncJobExeptionMessage))
                    action.ErrorMessage = asyncJobExeptionMessage; 
                    
            }
            logger.LogDebug($"End: ActionService UpdateSuccessfulAction(friendlyErrormessage: {friendlyErrormessage})");
        }

        public async Task FinishSuccessfulApplyUpgradeAction(Action finishedAction){
            logger.LogDebug($"Begin: ActionService FinishSuccessfulApplyUpgradeAction(finishedAction Id: {finishedAction.Id})");

            if(!String.IsNullOrEmpty(finishedAction.TargetEnvironmentNavigation.ConnectionsOwner) && finishedAction.SolutionNavigation.EnableWorkflows == true)
                await this.solutionService.AddEnableFlowsAction((int)finishedAction.Solution, finishedAction.TargetEnvironment, finishedAction.CreatedByNavigation.MsId);

            this.UpdateSuccessfulAction(finishedAction);

            logger.LogDebug($"End: ActionService FinishSuccessfulApplyUpgradeAction");
        }
    }
}