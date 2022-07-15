using System;
using System.Threading;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Action = at.D365.PowerCID.Portal.Data.Models.Action;

namespace at.D365.PowerCID.Portal.Services
{
    public class ActionService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly SolutionHistoryService solutionHistoryService;

        public ActionService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var scope = serviceProvider.CreateScope();

            this.solutionHistoryService = scope.ServiceProvider.GetRequiredService<SolutionHistoryService>();
        }

        public void UpdateSuccessfulAction(Action action)
        {
            action.Status = 3;
            action.Result = 1;
            action.FinishTime = DateTime.Now;
        }

        public async Task UpdateFailedAction(Action action, string friendlyErrormessage, AsyncJob asyncJobForExeptionMessage = null)
        {
            action.Status = 3;
            action.Result = 2;
            action.FinishTime = DateTime.Now;

            action.ErrorMessage = friendlyErrormessage;
            if (action.ErrorMessage == String.Empty && asyncJobForExeptionMessage != null)
            {
                action.ErrorMessage = await this.solutionHistoryService.GetExceptionMessage(asyncJobForExeptionMessage);
            }
        }
    }
}