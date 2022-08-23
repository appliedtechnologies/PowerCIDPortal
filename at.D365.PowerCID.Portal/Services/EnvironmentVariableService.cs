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

namespace at.D365.PowerCID.Portal.Services
{
    public class EnvironmentVariableService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly IDownstreamWebApi downstreamWebApi;
        private readonly IConfiguration configuration;

        public EnvironmentVariableService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var scope = serviceProvider.CreateScope();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.downstreamWebApi = scope.ServiceProvider.GetRequiredService<IDownstreamWebApi>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        }

        public async Task CleanEnvironmentVariables(int applicationId){
            var existsingEnvironmentVariablesInDataverse = await this.GetExistsingEnvironmentVariablesFromDataverse(applicationId);

            foreach (var environmentVariable in this.dbContext.EnvironmentVariables.Where(e => e.Application == applicationId))
            {
                if(!existsingEnvironmentVariablesInDataverse.Any(x => x.MsId == environmentVariable.MsId))
                    this.dbContext.EnvironmentVariables.Remove(environmentVariable);
            }

            await this.dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<EnvironmentVariable>> GetExistsingEnvironmentVariablesFromDataverse(int applicationId){
            Application application = await this.dbContext.Applications.FindAsync(applicationId);

            List<EnvironmentVariable> environmentVariables = new List<EnvironmentVariable>();
            var basicUrl = application.DevelopmentEnvironmentNavigation.BasicUrl;
            var tenantMsId = application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId;

            foreach (Solution solution in application.Solutions.Reverse())
            {
                var solutionComponents = await this.GetSolutionComponentsFromDataverse(solution.MsId, basicUrl);
                var environmentVariablesOfSolution = await this.GetEnvironemntVariablesBySolutionComponents(solutionComponents, applicationId, basicUrl, tenantMsId);
                environmentVariables.AddRange(environmentVariablesOfSolution.Where(e => environmentVariables.All(x => e.MsId != x.MsId)));

                if(!solution.IsPatch())
                    break;
            } 

            return environmentVariables;
        }

        public async Task<int> GetStatus(int applicationId, int environmentId)
        {
            //status 0=incomplete configuration;1=complete configuration 
            var existingEnvironmentVariables = await this.GetExistsingEnvironmentVariablesFromDataverse(applicationId);
            var existingEnvironmentVariablesMsIds = existingEnvironmentVariables.Select(e => e.MsId);

            if(existingEnvironmentVariables.Count() == 0)
                return 1;

            var environmentVariablesInDb = this.dbContext.EnvironmentVariables.Where(e => existingEnvironmentVariablesMsIds.Contains(e.MsId) && e.Application == applicationId);

            if(existingEnvironmentVariablesMsIds.All(e => environmentVariablesInDb.FirstOrDefault(x => x.MsId == e) != null) && environmentVariablesInDb.All(e => e.EnvironmentVariableEnvironments.Any(x => x.Environment == environmentId && x.Value != null && x.Value != String.Empty)))
                return 1;

            return 0;
        }

        private async Task<IEnumerable<EnvironmentVariable>> GetEnvironemntVariablesBySolutionComponents(EntityCollection solutionComponents, int applicationId, string basicUrl, Guid tenantMsId){
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

            return environmentVariables;
        }
        
        private async Task<EnvironmentVariable> GetEnvironmentVariableFromDataverse(Guid environmentVariableMsId, string basicUrl){
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                Entity response = await dataverseClient.RetrieveAsync("environmentvariabledefinition", environmentVariableMsId, new ColumnSet("displayname", "schemaname"));
                var environmentVariable = new EnvironmentVariable
                {
                    DisplayName = (string)response["displayname"],
                    LogicalName = (string)response["schemaname"],
                    MsId = environmentVariableMsId
                };

                return environmentVariable;
            }
        }

        private async Task<EntityCollection> GetSolutionComponentsFromDataverse(Guid solutionMsId, string basicUrl){
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                var query = new QueryExpression("solutioncomponent"){
                    ColumnSet = new ColumnSet("solutionid", "componenttype", "objectid"),
                };
                query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionMsId);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);
                return response; 
            }
        }
    }
}