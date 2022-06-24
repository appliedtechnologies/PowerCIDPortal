using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
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
                var solutionComponents = await this.GetSolutionComponentsFromDataverse(solution.MsId, basicUrl, tenantMsId);
                var environmentVariablesOfSolution = await this.GetEnvironemntVariablesBySolutionComponents(solutionComponents, applicationId, basicUrl, tenantMsId);
                environmentVariables.AddRange(environmentVariablesOfSolution.Except(environmentVariables));

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

            var environmentVariablesInDb = this.dbContext.EnvironmentVariables.Where(e => existingEnvironmentVariablesMsIds.Contains(e.MsId));

            if(existingEnvironmentVariablesMsIds.All(e => environmentVariablesInDb.FirstOrDefault(x => x.MsId == e) != null) && environmentVariablesInDb.All(e => e.EnvironmentVariableEnvironments.Any(x => x.Environment == environmentId && x.Value != null && x.Value != String.Empty)))
                return 1;

            return 0;
        }

        private async Task<IEnumerable<EnvironmentVariable>> GetEnvironemntVariablesBySolutionComponents(JToken solutionComponents, int applicationId, string basicUrl, Guid tenantMsId){
            List<EnvironmentVariable> environmentVariables = new List<EnvironmentVariable>();
            foreach (var solutionComponent in solutionComponents)
            {
                if((int)solutionComponent["componenttype"] == 380) //380 is type of a environment variable definition
                {
                    var environmentVariableId = new Guid((string)solutionComponent["objectid"]);
                    
                    var environmentVariable = await this.GetEnvironmentVariableFromDataverse(environmentVariableId, basicUrl, tenantMsId);
                    environmentVariable.Application = applicationId;
                    environmentVariables.Add(environmentVariable);
                }
            }

            return environmentVariables;
        }
        
        private async Task<EnvironmentVariable> GetEnvironmentVariableFromDataverse(Guid environmentVariableMsId, string basicUrl, Guid tenandMsId){
            var responseEnvironmentVariable = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{tenandMsId}";
                options.BaseUrl = basicUrl  + options.BaseUrl;
                options.RelativePath = $"/environmentvariabledefinitions?$filter=environmentvariabledefinitionid eq '{environmentVariableMsId}'";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{basicUrl}/.default";
            });

            var environmentVariableJToken = (await responseEnvironmentVariable.Content.ReadAsAsync<JObject>())["value"][0];
            var environmentVariable = new EnvironmentVariable
            {
                DisplayName = (string)environmentVariableJToken["displayname"],
                LogicalName = (string)environmentVariableJToken["schemaname"],
                MsId = environmentVariableMsId
            };

            return environmentVariable;
        }

        private async Task<JToken> GetSolutionComponentsFromDataverse(Guid solutionMsId, string basicUrl, Guid tenantMsId){
            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{tenantMsId}";
                options.BaseUrl = basicUrl  + options.BaseUrl;
                options.RelativePath = $"/solutioncomponents?$filter=_solutionid_value eq '{solutionMsId}'";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{basicUrl}/.default";
            });

            return (await response.Content.ReadAsAsync<JObject>())["value"];
        }
    }
}