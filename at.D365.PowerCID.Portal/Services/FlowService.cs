using System;
using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace at.D365.PowerCID.Portal.Services
{
    public class FlowService
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IConfiguration configuration;
        private readonly SolutionService solutionService;
        private readonly UserService userService;

        public FlowService(IServiceProvider serviceProvider, ILogger<FlowService> logger)
        {
            this.serviceProvider = serviceProvider;

            var scope = serviceProvider.CreateScope();

            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.userService = scope.ServiceProvider.GetRequiredService<UserService>();
            this.solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
            this.logger = logger;
        }

        public async Task<string> EnableAllCloudFlows(string solutionUniqueName, string connectionsOwnerDomainName, string basicUrlTargetSystem, string basicUrlDevelopmentSystem){
            logger.LogDebug($"Begin: FlowService EnableAllCloudFlows(solutionUniqueName: {solutionUniqueName}, connectionsOwnerDomainName: {connectionsOwnerDomainName}, basicUrlTargetSystem: {basicUrlTargetSystem}, basicUrlDevelopmentSystem: {basicUrlDevelopmentSystem})");

            string errorLog = "";

            var solutionMsIdTargetSystem = await this.solutionService.GetSolutionIdByUniqueName(solutionUniqueName, basicUrlTargetSystem);
            var solutionMsIdDevelopmentSystem = await this.solutionService.GetSolutionIdByUniqueName(solutionUniqueName, basicUrlDevelopmentSystem); 
            var connectionOwnerMsId = await this.userService.GetSystemUserId(connectionsOwnerDomainName, basicUrlTargetSystem);
            var cloudFlowsTargetEnvironment = await this.GetCloudFlows(solutionMsIdTargetSystem, basicUrlTargetSystem);

            using(var dataverseClient = new ServiceClient(new Uri(basicUrlTargetSystem), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                dataverseClient.CallerId = connectionOwnerMsId;
                foreach(var workflowTargetSystem in cloudFlowsTargetEnvironment.Entities){
                    try{
                        var workflowDevelopmentSystem = await this.GetCloudFlow(workflowTargetSystem.Id, basicUrlDevelopmentSystem);
                        if((((OptionSetValue)workflowDevelopmentSystem["statuscode"]).Value == 2 || ((OptionSetValue)workflowDevelopmentSystem["statecode"]).Value == 1) && (((OptionSetValue)workflowTargetSystem["statuscode"]).Value != 2 || ((OptionSetValue)workflowTargetSystem["statecode"]).Value != 1)){
                            var updatedWorkflow = new Entity(workflowTargetSystem.LogicalName, workflowTargetSystem.Id);
                            updatedWorkflow["statecode"] = 1;
                            updatedWorkflow["statuscode"] = 2;
                            await dataverseClient.UpdateAsync(updatedWorkflow);
                        }
                    }
                    catch(Exception e){
                        if(!String.IsNullOrEmpty(errorLog))
                            errorLog += " --- ";

                        errorLog += $"could not enable flow {workflowTargetSystem.Id} ({e.Message})";

                        continue;
                    }
                }
            }

            logger.LogDebug($"End: FlowService EnableAllCloudFlows(return {errorLog})");

            return errorLog;
        }

        private async Task<EntityCollection> GetCloudFlows(Guid solutionMsId, string basicUrl){
            logger.LogDebug($"Begin: FlowService GetCloudFlows(solutionMsId: {solutionMsId}, basicUrl: {basicUrl})");

            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                var query = new QueryExpression("workflow"){
                    ColumnSet = new ColumnSet("statuscode", "statecode"),
                };

                query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionMsId);
                query.Criteria.AddCondition("category", ConditionOperator.Equal, 5);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                logger.LogDebug($"End: FlowService GetCloudFlows(return EntityColletion Entity Count {response.Entities.Count})");

                return response;
            }
        }

        private async Task<Entity> GetCloudFlow(Guid flowMsId, string basicUrl){
            logger.LogDebug($"Begin: FlowService GetCloudFlow(flowMsId: {flowMsId}, basicUrl: {basicUrl})");

            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                Entity response = await dataverseClient.RetrieveAsync("workflow", flowMsId, new ColumnSet("statuscode", "statecode"));

                logger.LogDebug($"End: FlowService GetCloudFlow()");

                return response;
            }
        }
    }
}