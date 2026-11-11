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
        private readonly atPowerCIDContext dbContext;
        private readonly SolutionService solutionService;
        private readonly SolutionHistoryService solutionHistoryService;

        public ActionService(
            ILogger<ActionService> logger,
            atPowerCIDContext dbContext,
            SolutionHistoryService solutionHistoryService,
            SolutionService solutionService)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.solutionHistoryService = solutionHistoryService;
            this.solutionService = solutionService;
        }

        public void UpdateSuccessfulAction(Action action)
        {
            logger.LogDebug($"Begin: ActionService UpdateSuccessfulAction()");

            action.Status = 3;
            action.Result = 1;
            action.FinishTime = DateTime.Now;

            logger.LogDebug($"End: ActionService UpdateSuccessfulAction()");
        }

        public async Task UpdateFailedAction(
            Action action,
            string friendlyErrormessage,
            AsyncJob asyncJobForExceptionMessage = null)
        {
            logger.LogDebug("Begin: ActionService UpdateFailedAction(actionId: {ActionId})", action.Id);

            action.Status = 3;
            action.Result = 2;
            action.FinishTime = DateTime.Now;

            action.ErrorMessage = friendlyErrormessage;
            if ((action.ErrorMessage == String.Empty || action.ErrorMessage == "An unexpected error occurred.")
                && asyncJobForExceptionMessage != null)
            {
                try
                {
                    var asyncJobExceptionMessage =
                        await this.solutionHistoryService.GetExceptionMessage(asyncJobForExceptionMessage);
                    if (!String.IsNullOrEmpty(asyncJobExceptionMessage))
                        action.ErrorMessage = asyncJobExceptionMessage;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Could not retrieve solution history details for failed action {ActionId}.",
                        action.Id);
                }
            }

            logger.LogDebug("End: ActionService UpdateFailedAction(actionId: {ActionId})", action.Id);
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