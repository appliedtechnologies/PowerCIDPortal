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
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class ConnectionReferenceService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly IDownstreamWebApi downstreamWebApi;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;
        public ConnectionReferenceService(IServiceProvider serviceProvider, ILogger<ConnectionReferenceService> logger)
        {
            this.serviceProvider = serviceProvider;
            var scope = serviceProvider.CreateScope();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.downstreamWebApi = scope.ServiceProvider.GetRequiredService<IDownstreamWebApi>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.logger = logger;
        }

        public async Task<IEnumerable<ConnectionReference>> GetExistsingConnectionReferencesFromDataverse(int applicationId){

            logger.LogDebug($"Begin: ConnectionReferenceService GetExistsingConnectionReferencesFromDataverse(applicationId = {applicationId})");

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

            logger.LogDebug($"End: ConnectionReferenceService GetExistsingConnectionReferencesFromDataverse()");

            return connectionReferences;
        }

        public async Task CleanConnectionReferences(int applicationId){

            logger.LogDebug($"Begin: ConnectionReferenceService CleanConnectionReferences(applicationId = {applicationId})");

            var existsingConnectionReferencesInDataverse = await this.GetExistsingConnectionReferencesFromDataverse(applicationId);

            foreach (var connectionReference in this.dbContext.ConnectionReferences.Where(e => e.Application == applicationId))
            {
                if(!existsingConnectionReferencesInDataverse.Any(x => x.MsId == connectionReference.MsId))
                    this.dbContext.ConnectionReferences.Remove(connectionReference);
            }

            logger.LogDebug($"End: ConnectionReferenceService CleanConnectionReferences(applicationId = {applicationId})");

            await this.dbContext.SaveChangesAsync();
        }

        public async Task<int> GetStatus(int applicationId, int environmentId)
        {
            logger.LogDebug($"Begin: ConnectionReferenceService GetStatus(applicationId = {applicationId}, environmentId = {environmentId})");

            //status 0=incomplete configuration;1=complete configuration 
            var existingConnectionReferences = await this.GetExistsingConnectionReferencesFromDataverse(applicationId);
            var existingConnectionReferencesMsIds = existingConnectionReferences.Select(e => e.MsId);

            if(existingConnectionReferences.Count() == 0){
                logger.LogInformation("Configuration completed:  ConnectionReferenceService GetStatus()");
                return 1;
            }
                

            var connectionReferencesInDb = this.dbContext.ConnectionReferences.Where(e => existingConnectionReferencesMsIds.Contains(e.MsId) && e.Application == applicationId);

            if(existingConnectionReferencesMsIds.All(e => connectionReferencesInDb.FirstOrDefault(x => x.MsId == e) != null) && connectionReferencesInDb.All(e => e.ConnectionReferenceEnvironments.Any(x => x.Environment == environmentId && x.ConnectionId != null && x.ConnectionId != String.Empty))){
                logger.LogInformation("Configuration completed:  ConnectionReferenceService GetStatus()");
                return 1;
            }
               
            logger.LogInformation("Configuration incompleted:  ConnectionReferenceService GetStatus()");

            return 0;
        }

        private async Task<IEnumerable<ConnectionReference>> GetConnectionReferencesBySolutionComponents(JToken solutionComponents, int applicationId, string basicUrl, Guid tenantMsId){

            logger.LogDebug($"Begin: ConnectionReferenceService GetConnectionReferencesBySolutionComponents(applicationId: {applicationId}, basicUrl: {basicUrl}, tenantMsId: {tenantMsId.ToString()})");

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

                logger.LogDebug($"End: ConnectionReferenceService GetConnectionReferencesBySolutionComponents()");

            return connectionReferences;
        }
        
        private async Task<ConnectionReference> GetConnectionReferenceFromDataverse(Guid connectionReferenceMsId, string basicUrl, Guid tenandMsId){

            logger.LogDebug($"Begin: ConnectionReferenceService GetConnectionReferenceFromDataverse(connectionReferenceMsId: {connectionReferenceMsId.ToString()}, basicUrl: {basicUrl}, tenandMsId: {tenandMsId.ToString()})");

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

            logger.LogDebug($"End: ConnectionReferenceService GetConnectionReferenceFromDataverse()");

            return connectionReference;
        }

        private async Task<JToken> GetSolutionComponentsFromDataverse(Guid solutionMsId, string basicUrl, Guid tenantMsId){

            logger.LogDebug($"Begin: ConnectionReferenceService GetSolutionComponentsFromDataverse(solutionMsId: {solutionMsId.ToString()}, basicUrl: {basicUrl}, tenantMsId: {tenantMsId.ToString()})");

            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{tenantMsId}";
                options.BaseUrl = basicUrl  + options.BaseUrl;
                options.RelativePath = $"/solutioncomponents?$filter=_solutionid_value eq '{solutionMsId}'";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{basicUrl}/.default";
            });

            logger.LogDebug($"End: ConnectionReferenceService GetSolutionComponentsFromDataverse()");

            return (await response.Content.ReadAsAsync<JObject>())["value"];
        }
    }
}