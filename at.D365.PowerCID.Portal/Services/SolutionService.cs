using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit.GraphQL;
using Action = at.D365.PowerCID.Portal.Data.Models.Action;

namespace at.D365.PowerCID.Portal.Services
{
    public class SolutionService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly ConnectionReferenceService connectionReferenceService;
        private readonly EnvironmentVariableService environmentVariableService;
        private readonly IConfiguration configuration;
        private readonly SolutionHistoryService solutionHistoryService;

        public SolutionService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var scope = serviceProvider.CreateScope();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.connectionReferenceService = scope.ServiceProvider.GetRequiredService<ConnectionReferenceService>();
            this.environmentVariableService = scope.ServiceProvider.GetRequiredService<EnvironmentVariableService>();
            this.solutionHistoryService = scope.ServiceProvider.GetRequiredService<SolutionHistoryService>();
        }

        public async Task<Data.Models.Action> AddExportAction(int key, Guid tenantMsIdCurrentUser, Guid msIdCurrentUser, bool exportOnly, int targetEnvironmentForImport = 0)
        {
            if (!exportOnly && targetEnvironmentForImport != 0)
            {
                User user = this.dbContext.Users.First(e => e.MsId == msIdCurrentUser);
                await this.CheckImportPermission(user.Id, targetEnvironmentForImport);
            }

            Solution solution = dbContext.Solutions.First(e => e.Id == key);

            Data.Models.Action newAction = new Data.Models.Action
            {
                Name = $"{solution.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                TargetEnvironment = solution.ApplicationNavigation.DevelopmentEnvironment,
                Type = 1,
                Status = 1,
                StartTime = DateTime.Now,
                Solution = solution.Id,
                ExportOnly = exportOnly,
                ImportTargetEnvironment = targetEnvironmentForImport
            };

            dbContext.Add(newAction);
            await dbContext.SaveChangesAsync(msIdCurrentUser: msIdCurrentUser);
            return newAction;
        }

        public async Task<Data.Models.Action> AddImportAction(int key, int targetEnvironmentId, Guid msIdCurrentUser)
        {
            Solution solution = dbContext.Solutions.First(e => e.Id == key);
            User user = this.dbContext.Users.First(e => e.MsId == msIdCurrentUser);

            await this.CheckImportPermission(user.Id, targetEnvironmentId);

            Data.Models.Action newAction = new Data.Models.Action
            {
                Name = $"{solution.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                TargetEnvironment = targetEnvironmentId,
                Type = 2,
                Status = 1,
                StartTime = DateTime.Now,
                Solution = solution.Id
            };

            dbContext.Add(newAction);
            await dbContext.SaveChangesAsync(msIdCurrentUser: msIdCurrentUser);
            return newAction;
        }

        public async Task<Data.Models.Action> AddApplyUpgradeAction(int key, int targetEnvironmentId, Guid msIdCurrentUser)
        {
            Solution solution = dbContext.Solutions.First(e => e.Id == key);
            User user = this.dbContext.Users.First(e => e.MsId == msIdCurrentUser);

            await this.CheckImportPermission(user.Id, targetEnvironmentId);

            Data.Models.Action newAction = new Data.Models.Action
            {
                Name = $"{solution.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                TargetEnvironment = targetEnvironmentId,
                Type = 3,
                Status = 1,
                StartTime = DateTime.Now,
                Solution = solution.Id
            };

            dbContext.Add(newAction);
            await dbContext.SaveChangesAsync(msIdCurrentUser: msIdCurrentUser);
            return newAction;
        }

        public async Task CreateUpgrade(Upgrade upgrade, string version)
        {
            Application application = this.dbContext.Applications.FirstOrDefault(e => e.Id == upgrade.Application);
            string lastSolution = version == null ? application.Solutions.OrderByDescending(e => e.CreatedOn).FirstOrDefault()?.Version : version;
            if(upgrade.Version == null)
                upgrade.Version = lastSolution == null ? VersionHelper.GetNextMinorVersion("1.0.0.0") : VersionHelper.GetNextMinorVersion(lastSolution);

            await this.CreateUpgradeInDataverse(application.SolutionUniqueName, application.Name, application.DevelopmentEnvironmentNavigation.BasicUrl, application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId, upgrade);
            upgrade.UrlMakerportal = $"https://make.powerapps.com/environments/{application.DevelopmentEnvironmentNavigation.MsId}/solutions/{upgrade.MsId}";
        }

        public async Task<AsyncJob> StartExportInDataverse(string solutionUniqueName, bool isManaged, string basicUrl, Data.Models.Action action, Guid tenantMsIdCurrentUser, int environmentId, int targetEnvironment)
        {
            ExportSolutionAsyncRequest exportSolutionRequest = new ExportSolutionAsyncRequest{
                Managed = isManaged,
                SolutionName = solutionUniqueName
            };

            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                try {
                    ExportSolutionAsyncResponse response = (ExportSolutionAsyncResponse)await dataverseClient.ExecuteAsync(exportSolutionRequest);
                    AsyncJob asyncJob = new AsyncJob
                    {
                        AsyncOperationId = response.AsyncOperationId,
                        JobId = response.ExportJobId,
                        IsManaged = isManaged,
                        Action = action.Id
                    };

                    return asyncJob;
                }
                catch(Exception e){
                    throw new Exception("Could not start Export in Dataverse");
                }
            }
        }

        public async Task<AsyncJob> StartImportInDataverse(byte[] solutionFileData, Action action)
        {
            bool isPatch = this.dbContext.Solutions.FirstOrDefault(s => s.Id == action.Solution).GetType().Name.Contains("Patch");
            Upgrade upgrade;
            upgrade = isPatch == false ? (Upgrade)action.SolutionNavigation : default;
            bool existsSolutionInTargetEnvironment = await ExistsSolutionInTargetEnvironment(action.SolutionNavigation.UniqueName, action.TargetEnvironmentNavigation.BasicUrl, action.TargetEnvironmentNavigation.TenantNavigation.MsId.ToString());

            EntityCollection solutionComponentParameters = await this.GetSolutionComponentsForImport(action.TargetEnvironment, action.SolutionNavigation.Application);

            ImportSolutionAsyncRequest importSolutionAsyncRequest = new ImportSolutionAsyncRequest{
                CustomizationFile = solutionFileData,
                OverwriteUnmanagedCustomizations = action.SolutionNavigation.OverwriteUnmanagedCustomizations ?? true,
                PublishWorkflows = action.SolutionNavigation.EnableWorkflows ?? true,
                HoldingSolution = !isPatch && existsSolutionInTargetEnvironment == true,
                ComponentParameters = solutionComponentParameters
            };

            using(var dataverseClient = new ServiceClient(new Uri(action.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                ImportSolutionAsyncResponse response = (ImportSolutionAsyncResponse)await dataverseClient.ExecuteAsync(importSolutionAsyncRequest);

                AsyncJob asyncJob = new AsyncJob
                {
                    AsyncOperationId = response.AsyncOperationId,
                    JobId = Guid.Parse(response.ImportJobKey),
                    IsManaged = true,
                    Action = action.Id
                };

                return asyncJob;
            }
        }

        public async Task<AsyncJob> DeleteAndPromoteInDataverse(Action action)
        {
            DeleteAndPromoteRequest deleteAndPromoteRequest = new DeleteAndPromoteRequest{
                UniqueName = action.SolutionNavigation.UniqueName
            };

            using(var dataverseClient = new ServiceClient(new Uri(action.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                DeleteAndPromoteResponse response = (DeleteAndPromoteResponse)await dataverseClient.ExecuteAsync(deleteAndPromoteRequest);
                Guid solutionHistoryId = await this.solutionHistoryService.GetIdForDeleteAndPromote(action.SolutionNavigation, action.TargetEnvironmentNavigation.BasicUrl);

                AsyncJob asyncJob = new AsyncJob
                {
                    JobId = solutionHistoryId,
                    IsManaged = true,
                    Action = action.Id
                };

                return asyncJob;
            }
        }

        public async Task<string> DownloadSolutionFileFromDataverse(AsyncJob asyncJob)
        {
            DownloadSolutionExportDataRequest downloadSolutionExportDataRequest = new DownloadSolutionExportDataRequest{
                ExportJobId = (Guid)asyncJob.JobId
            };

            using(var dataverseClient = new ServiceClient(new Uri(asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                DownloadSolutionExportDataResponse response = (DownloadSolutionExportDataResponse)await dataverseClient.ExecuteAsync(downloadSolutionExportDataRequest);
                string base64 = Convert.ToBase64String(response.ExportSolutionFile);
                return base64;
            }
        }

        private async Task CheckImportPermission(int userId, int environmentId)
        {
            UserEnvironment userEnvironment = await this.dbContext.UserEnvironments.FindAsync(userId, environmentId);
            if (userEnvironment == null)
                throw new Exception("User does not have permission within PowerCID Portal to import/apply upgrade on target environment. Your administrator can assign the permission via Power CID Portal user management.");
        }

        private async Task CreateUpgradeInDataverse(string solutionUniqueName, string solutionDisplayName, string basicUrl, Guid tenantMsId, Upgrade upgrade)
        {
            CloneAsSolutionRequest cloneAsSolutionRequest = new CloneAsSolutionRequest{
                DisplayName = solutionDisplayName,
                ParentSolutionUniqueName = solutionUniqueName,
                VersionNumber = upgrade.Version
            };

            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                try {
                    CloneAsSolutionResponse response = (CloneAsSolutionResponse)await dataverseClient.ExecuteAsync(cloneAsSolutionRequest);
                    upgrade.MsId = response.SolutionId;
                    upgrade.UniqueName = solutionUniqueName;
                }
                catch(Exception e){
                    throw new Exception($"Could not create Upgrade in Dataverse: {e.Message}");
                }
            }
        }

        private async Task<EntityCollection> GetSolutionComponentsForImport(int environmentId, int applicationId){
            EntityCollection solutionComponentParameters = new EntityCollection();

            var connectionReferenceEntities = await this.GetConnectionReferencesForImport(environmentId, applicationId);
            var environmentVariableEntities = await this.GetEnvironmentVariablesForImport(environmentId, applicationId);
            solutionComponentParameters.Entities.AddRange(connectionReferenceEntities.Entities);
            solutionComponentParameters.Entities.AddRange(environmentVariableEntities.Entities);

            if(solutionComponentParameters.Entities.Count == 0)
                return null;

            return solutionComponentParameters;
        }

        private async Task<EntityCollection> GetEnvironmentVariablesForImport(int environmentId, int applicationId){
            await this.environmentVariableService.CleanEnvironmentVariables(applicationId);

            EntityCollection environmentVariableEntities = new EntityCollection();

            var environmentVariableEnvironments = this.dbContext.EnvironmentVariableEnvironments.Where(e => e.Environment == environmentId && e.EnvironmentVariableNavigation.Application == applicationId).ToList();
            foreach (EnvironmentVariableEnvironment environmentVariableEnvironment in environmentVariableEnvironments)
            {
                Entity connRecord = new Entity("environmentvariablevalue");
                connRecord.Attributes.Add("schemaname", environmentVariableEnvironment.EnvironmentVariableNavigation.LogicalName);
                connRecord.Attributes.Add("value", environmentVariableEnvironment.Value);
                environmentVariableEntities.Entities.Add(connRecord);
            }

            return environmentVariableEntities;
        }

        private async Task<EntityCollection> GetConnectionReferencesForImport(int environmentId, int applicationId){
            await this.connectionReferenceService.CleanConnectionReferences(applicationId);

            EntityCollection connectionReferenceEntities = new EntityCollection();

            var connectionReferenceEnvironments = this.dbContext.ConnectionReferenceEnvironments.Where(e => e.Environment == environmentId && e.ConnectionReferenceNavigation.Application == applicationId).ToList();
            foreach (ConnectionReferenceEnvironment connectionReferenceEnvironment in connectionReferenceEnvironments)
            {
                Entity connRecord = new Entity("connectionreference");
                connRecord.Attributes.Add("connectionreferencedisplayname", connectionReferenceEnvironment.ConnectionReferenceNavigation.DisplayName);
                connRecord.Attributes.Add("connectionreferencelogicalname", connectionReferenceEnvironment.ConnectionReferenceNavigation.LogicalName);
                connRecord.Attributes.Add("connectorid", connectionReferenceEnvironment.ConnectionReferenceNavigation.ConnectorId);
                connRecord.Attributes.Add("connectionid", connectionReferenceEnvironment.ConnectionId);
                connectionReferenceEntities.Entities.Add(connRecord);
            }

            return connectionReferenceEntities;
        }

        private async Task<bool> ExistsSolutionInTargetEnvironment(string solutionUniqueName, string basicUrl, string tenantMsId)
        {
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                var query = new QueryExpression("solution"){
                    ColumnSet = new ColumnSet("uniquename"),
                };
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, new [] {solutionUniqueName});

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                return response.Entities.Count > 0;
            }
        }
    }
}