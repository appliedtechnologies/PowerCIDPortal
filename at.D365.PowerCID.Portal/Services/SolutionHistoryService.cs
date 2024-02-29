using System;
using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class SolutionHistoryService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public SolutionHistoryService(ILogger<SolutionHistoryService> logger, IConfiguration configuration)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<Entity> GetEntryById(Guid solutionHistoryId, string basicUrl)
        {
            logger.LogDebug($"Begin: SolutionHistoryService GetEntryById(solutionHistoryId: {solutionHistoryId.ToString()}, basicUrl: {basicUrl})");

            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                Entity response = await dataverseClient.RetrieveAsync("msdyn_solutionhistory", solutionHistoryId, new ColumnSet("msdyn_status", "msdyn_endtime", "msdyn_result", "msdyn_exceptionmessage"));

                logger.LogDebug($"End: SolutionHistoryService GetEntryById(solutionHistoryId: {solutionHistoryId.ToString()}, basicUrl: {basicUrl})");

                return response;
            }
        }

        public async Task<string> GetExceptionMessage(AsyncJob asyncJob)
        {
            logger.LogDebug($"Begin: SolutionHistoryService GetExceptionMessage(asyncJob BasicUrl: {asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl})");

            using (var dataverseClient = new ServiceClient(new Uri(asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                var query = new QueryExpression("msdyn_solutionhistory")
                {
                    ColumnSet = new ColumnSet("msdyn_exceptionmessage"),
                    PageInfo = new PagingInfo()
                    {
                        Count = 1,
                        PageNumber = 1
                    }
                };
                query.Criteria.AddCondition("msdyn_name", ConditionOperator.Equal, new[] { asyncJob.ActionNavigation.SolutionNavigation.UniqueName });
                query.Criteria.AddCondition("msdyn_solutionversion", ConditionOperator.Equal, new[] { asyncJob.ActionNavigation.SolutionNavigation.Version });

                query.AddOrder("msdyn_starttime", OrderType.Descending);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                logger.LogDebug($"Begin: SolutionHistoryService GetExceptionMessage(asyncJob BasicUrl: {asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl})");

                return (string)response.Entities.FirstOrDefault()?["msdyn_exceptionmessage"];
            }
        }

        public async Task<Guid> GetIdForDeleteAndPromote(Solution solution, string basicUrl)
        {
            logger.LogDebug($"Begin: SolutionHistoryService GetIdForDeleteAndPromote(solution UniqueName: {solution.UniqueName}, basicUrl: {basicUrl})");

            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                var query = new QueryExpression("msdyn_solutionhistory")
                {
                    ColumnSet = new ColumnSet("msdyn_solutionhistoryid"),
                    PageInfo = new PagingInfo()
                    {
                        Count = 1,
                        PageNumber = 1
                    }
                };

                query.Criteria.AddCondition("msdyn_name", ConditionOperator.Equal, solution.UniqueName);
                query.Criteria.AddCondition("msdyn_operation", ConditionOperator.Equal, 1);

                query.AddOrder("msdyn_starttime", OrderType.Descending);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                var solutionHistoryEntry = response.Entities.FirstOrDefault();

                if(solutionHistoryEntry == null)
                    throw new Exception("Can not get Solution History entry for DeleteAndPromote");
                    
                logger.LogDebug($"End: SolutionHistoryService GetIdForDeleteAndPromote(solution UniqueName: {solution.UniqueName}, basicUrl: {basicUrl})");

                return (Guid)solutionHistoryEntry["msdyn_solutionhistoryid"];
            }
        }
    }
}