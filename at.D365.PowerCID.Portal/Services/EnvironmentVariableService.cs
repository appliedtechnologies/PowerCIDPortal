using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class EnvironmentVariableService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly IDownstreamWebApi downstreamWebApi;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public EnvironmentVariableService(IServiceProvider serviceProvider, ILogger<EnvironmentVariableService> logger){
            this.serviceProvider = serviceProvider;
            var scope = serviceProvider.CreateScope();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.downstreamWebApi = scope.ServiceProvider.GetRequiredService<IDownstreamWebApi>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.logger = logger;
        }

        public async Task CleanEnvironmentVariables(int applicationId){
            logger.LogDebug($"Begin: EnvironmentVariableService CleanEnvironmentVariables(applicationId: {applicationId})");

            var existsingEnvironmentVariablesInDataverse = await this.GetExistsingEnvironmentVariablesFromDataverse(applicationId);

            foreach (var environmentVariable in this.dbContext.EnvironmentVariables.Where(e => e.Application == applicationId))
            {
                if (!existsingEnvironmentVariablesInDataverse.Any(x => x.MsId == environmentVariable.MsId))
                    this.dbContext.EnvironmentVariables.Remove(environmentVariable);
            }

            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: EnvironmentVariableService CleanEnvironmentVariables(applicationId: {applicationId})");
        }

        public async Task<IEnumerable<EnvironmentVariable>> GetExistsingEnvironmentVariablesFromDataverse(int applicationId){
            logger.LogDebug($"Begin: EnvironmentVariableService GetExistsingEnvironmentVariablesFromDataverse(applicationId: {applicationId})");

            Application application = await this.dbContext.Applications.FindAsync(applicationId);

            List<EnvironmentVariable> environmentVariables = new List<EnvironmentVariable>();
            var basicUrl = application.DevelopmentEnvironmentNavigation.BasicUrl;
            var tenantMsId = application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId;

            foreach (Solution solution in application.Solutions.Reverse())
            {
                var solutionComponents = await this.GetSolutionComponentsFromDataverse(solution.MsId, basicUrl);
                var environmentVariablesOfSolution = await this.GetEnvironemntVariablesBySolutionComponents(solutionComponents, applicationId, basicUrl, tenantMsId);
                environmentVariables.AddRange(environmentVariablesOfSolution.Where(e => environmentVariables.All(x => e.MsId != x.MsId)));

                if (!solution.IsPatch())
                    break;
            }
            logger.LogDebug($"End: EnvironmentVariableService GetExistsingEnvironmentVariablesFromDataverse(applicationId: {applicationId})");

            return environmentVariables;
        }

        public async Task<int> GetStatus(int applicationId, int environmentId){
            logger.LogDebug($"Begin: EnvironmentVariableService GetStatus(applicationId: {applicationId}, environmentId: {environmentId})");
            //status 0=incomplete configuration;1=complete configuration 
            var existingEnvironmentVariables = await this.GetExistsingEnvironmentVariablesFromDataverse(applicationId);
            var existingEnvironmentVariablesMsIds = existingEnvironmentVariables.Select(e => e.MsId);

            if (existingEnvironmentVariables.Count() == 0)
            {
                logger.LogInformation("Configuration incompleted)");
                logger.LogDebug($"Begin: EnvironmentVariableService GetStatus(applicationId: {applicationId}, environmentId: {environmentId})");
                return 1;
            }


            var environmentVariablesInDb = this.dbContext.EnvironmentVariables.Where(e => existingEnvironmentVariablesMsIds.Contains(e.MsId) && e.Application == applicationId);

            if (existingEnvironmentVariablesMsIds.All(e => environmentVariablesInDb.FirstOrDefault(x => x.MsId == e) != null) && environmentVariablesInDb.All(e => e.EnvironmentVariableEnvironments.Any(x => x.Environment == environmentId && x.Value != null && x.Value != String.Empty)))
            {
                logger.LogInformation("Configuration incompleted)");
                logger.LogDebug($"Begin: EnvironmentVariableService GetStatus(applicationId: {applicationId}, environmentId: {environmentId})");
                return 1;
            }
            logger.LogInformation("Configuration completed)");
            logger.LogDebug($"Begin: EnvironmentVariableService GetStatus(applicationId: {applicationId}, environmentId: {environmentId})");

            return 0;
        }

        private async Task<IEnumerable<EnvironmentVariable>> GetEnvironemntVariablesBySolutionComponents(EntityCollection solutionComponents, int applicationId, string basicUrl, Guid tenantMsId){
            logger.LogDebug($"Begin: EnvironmentVariableService  GetEnvironemntVariablesBySolutionComponents(applicationId: {applicationId}, basicUrl: {basicUrl}, tenantMsId: {tenantMsId.ToString()})");

            List<EnvironmentVariable> environmentVariables = new List<EnvironmentVariable>();
            foreach (var solutionComponent in solutionComponents.Entities)
            {
                if(((OptionSetValue)solutionComponent["componenttype"]).Value == 380) //380 is type of a environment variable definition
                {
                    var environmentVariableMsId = (Guid)solutionComponent["objectid"];
                    
                    var environmentVariable = await this.GetEnvironmentVariableFromDataverse(environmentVariableMsId, basicUrl);
                    environmentVariable.Application = applicationId;
                    environmentVariables.Add(environmentVariable);
                }
            }
            logger.LogDebug($"End: EnvironmentVariableService  GetEnvironemntVariablesBySolutionComponents(applicationId: {applicationId}, basicUrl: {basicUrl}, tenantMsId: {tenantMsId.ToString()})");

            return environmentVariables;
        }
        
        private async Task<EnvironmentVariable> GetEnvironmentVariableFromDataverse(Guid environmentVariableMsId, string basicUrl){
            logger.LogDebug($"Begin: EnvironmentVariableService  GetEnvironmentVariableFromDataverse(environmentVariableMsId: {environmentVariableMsId.ToString()},basicUrl: {basicUrl})");

            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                Entity response = await dataverseClient.RetrieveAsync("environmentvariabledefinition", environmentVariableMsId, new ColumnSet("displayname", "schemaname"));
                var environmentVariable = new EnvironmentVariable
                {
                    DisplayName = (string)response["displayname"],
                    LogicalName = (string)response["schemaname"],
                    MsId = environmentVariableMsId
                };

                logger.LogDebug($"End: EnvironmentVariableService  GetEnvironmentVariableFromDataverse(environmentVariableMsId: {environmentVariableMsId.ToString()},basicUrl: {basicUrl})");

                return environmentVariable;
            }
        }

        private async Task<EntityCollection> GetSolutionComponentsFromDataverse(Guid solutionMsId, string basicUrl){
            logger.LogDebug($"Begin: EnvironmentVariableService GetSolutionComponentsFromDataverse(solutionMsId: {solutionMsId.ToString()}, basicUrl: {basicUrl})");
            
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                var query = new QueryExpression("solutioncomponent"){
                    ColumnSet = new ColumnSet("solutionid", "componenttype", "objectid"),
                };
                query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionMsId);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);
                
                logger.LogDebug($"End: EnvironmentVariableService GetSolutionComponentsFromDataverse(solutionMsId: {solutionMsId.ToString()}, basicUrl: {basicUrl})");

                return response; 
            }
        }
    }
}