using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
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
                var solutionComponents = await this.GetSolutionComponentsFromDataverse(solution.MsId, basicUrl);
                var connectionReferencesOfSolution = await this.GetConnectionReferencesBySolutionComponents(solutionComponents, applicationId, basicUrl);
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

        private async Task<IEnumerable<ConnectionReference>> GetConnectionReferencesBySolutionComponents(EntityCollection solutionComponents, int applicationId, string basicUrl){
            List<ConnectionReference> connectionReferences = new List<ConnectionReference>();
            foreach (var solutionComponent in solutionComponents.Entities)   
            {
                if(solutionComponent.FormattedValues["componenttype"] == null)
                {
                    try{
                        var connectionReferenceMsId = (Guid)solutionComponent["objectid"];
                    
                        var connectionReference = await this.GetConnectionReferenceFromDataverse(connectionReferenceMsId, basicUrl);
                        connectionReference.Application = applicationId;
                        connectionReferences.Add(connectionReference);
                    }
                    catch(FaultException e){
                        //TODO log not a connection reference, but this can be normal
                        continue;
                    }
                }
            }

            return connectionReferences;
        }
        
        private async Task<ConnectionReference> GetConnectionReferenceFromDataverse(Guid connectionReferenceMsId, string basicUrl){
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                Entity response = await dataverseClient.RetrieveAsync("connectionreference", connectionReferenceMsId, new ColumnSet("connectionreferencedisplayname", "connectionreferencelogicalname", "connectorid"));
                var connectionReference = new ConnectionReference
                {
                    DisplayName = (string)response["connectionreferencedisplayname"],
                    LogicalName = (string)response["connectionreferencelogicalname"],
                    ConnectorId = (string)response["connectorid"],
                    MsId = connectionReferenceMsId
                };

                return connectionReference;
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