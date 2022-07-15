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
    public class ConnectionReferenceService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly IDownstreamWebApi downstreamWebApi;
        private readonly IConfiguration configuration;
        public ConnectionReferenceService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var scope = serviceProvider.CreateScope();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.downstreamWebApi = scope.ServiceProvider.GetRequiredService<IDownstreamWebApi>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        }

        public async Task<IEnumerable<ConnectionReference>> GetExistsingConnectionReferencesFromDataverse(int applicationId){
            Application application = await this.dbContext.Applications.FindAsync(applicationId);

            List<ConnectionReference> connectionReferences = new List<ConnectionReference>();
            var basicUrl = application.DevelopmentEnvironmentNavigation.BasicUrl;
            var tenantMsId = application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId;

            foreach (Solution solution in application.Solutions.Reverse())
            {
                var solutionComponents = await this.GetSolutionComponentsFromDataverse(solution.MsId, basicUrl, tenantMsId);
                var connectionReferencesOfSolution = await this.GetConnectionReferencesBySolutionComponents(solutionComponents, applicationId, basicUrl, tenantMsId);
                connectionReferences.AddRange(connectionReferencesOfSolution.Where(e => connectionReferences.All(x => e.MsId != x.MsId)));

                if(!solution.IsPatch())
                    break;
            } 

            return connectionReferences;
        }

        public async Task CleanConnectionReferences(int applicationId){
            var existsingConnectionReferencesInDataverse = await this.GetExistsingConnectionReferencesFromDataverse(applicationId);

            foreach (var connectionReference in this.dbContext.ConnectionReferences.Where(e => e.Application == applicationId))
            {
                if(!existsingConnectionReferencesInDataverse.Any(x => x.MsId == connectionReference.MsId))
                    this.dbContext.ConnectionReferences.Remove(connectionReference);
            }

            await this.dbContext.SaveChangesAsync();
        }

        public async Task<int> GetStatus(int applicationId, int environmentId)
        {
            //status 0=incomplete configuration;1=complete configuration 
            var existingConnectionReferences = await this.GetExistsingConnectionReferencesFromDataverse(applicationId);
            var existingConnectionReferencesMsIds = existingConnectionReferences.Select(e => e.MsId);

            if(existingConnectionReferences.Count() == 0)
                return 1;

            var connectionReferencesInDb = this.dbContext.ConnectionReferences.Where(e => existingConnectionReferencesMsIds.Contains(e.MsId) && e.Application == applicationId);

            if(existingConnectionReferencesMsIds.All(e => connectionReferencesInDb.FirstOrDefault(x => x.MsId == e) != null) && connectionReferencesInDb.All(e => e.ConnectionReferenceEnvironments.Any(x => x.Environment == environmentId && x.ConnectionId != null && x.ConnectionId != String.Empty)))
                return 1;

            return 0;
        }

        private async Task<IEnumerable<ConnectionReference>> GetConnectionReferencesBySolutionComponents(JToken solutionComponents, int applicationId, string basicUrl, Guid tenantMsId){
            List<ConnectionReference> connectionReferences = new List<ConnectionReference>();
            foreach (var solutionComponent in solutionComponents)
            {
                if((int)solutionComponent["componenttype"] == 10029) //10029 is type of a connection reference´
                {
                    var connectionReferenceMsId = new Guid((string)solutionComponent["objectid"]);
                    
                    var connectionReference = await this.GetConnectionReferenceFromDataverse(connectionReferenceMsId, basicUrl, tenantMsId);
                    connectionReference.Application = applicationId;
                    connectionReferences.Add(connectionReference);
                }
            }

            return connectionReferences;
        }
        
        private async Task<ConnectionReference> GetConnectionReferenceFromDataverse(Guid connectionReferenceMsId, string basicUrl, Guid tenandMsId){
            var responseConnectionReference = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{tenandMsId}";
                options.BaseUrl = basicUrl  + options.BaseUrl;
                options.RelativePath = $"/connectionreferences?$filter=connectionreferenceid eq '{connectionReferenceMsId}'";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{basicUrl}/.default";
            });

            var connectionReferenceJToken = (await responseConnectionReference.Content.ReadAsAsync<JObject>())["value"][0];
            var connectionReference = new ConnectionReference
            {
                DisplayName = (string)connectionReferenceJToken["connectionreferencedisplayname"],
                LogicalName = (string)connectionReferenceJToken["connectionreferencelogicalname"],
                ConnectorId = (string)connectionReferenceJToken["connectorid"],
                MsId = connectionReferenceMsId
            };

            return connectionReference;
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