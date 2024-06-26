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
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class SolutionService
    {
        private readonly ILogger logger;
        private readonly atPowerCIDContext dbContext;
        private readonly ConnectionReferenceService connectionReferenceService;
        private readonly EnvironmentVariableService environmentVariableService;
        private readonly SolutionHistoryService solutionHistoryService;
        private readonly IConfiguration configuration;

        public SolutionService(ILogger<SolutionService> logger, atPowerCIDContext dbContext, ConnectionReferenceService connectionReferenceService, EnvironmentVariableService environmentVariableService, SolutionHistoryService solutionHistoryService, IConfiguration configuration)
        {
            this.logger = logger;

            this.dbContext = dbContext;
            this.configuration = configuration;
            this.connectionReferenceService = connectionReferenceService;
            this.environmentVariableService = environmentVariableService;
            this.solutionHistoryService = solutionHistoryService;
        }

        public async Task<Data.Models.Action> AddExportAction(int key, Guid tenantMsIdCurrentUser, Guid msIdCurrentUser, bool exportOnly, int targetEnvironmentForImport = 0)
        {
            logger.LogDebug($"Begin: SolutionService AddExportAction(key: {key}, tenantMsIdCurrentUser: {tenantMsIdCurrentUser.ToString()}, msIdCurrentUser: {msIdCurrentUser.ToString()} exportOnly: {exportOnly})");

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

            logger.LogDebug($"End: SolutionService AddExportAction(key: {key}, tenantMsIdCurrentUser: {tenantMsIdCurrentUser.ToString()}, msIdCurrentUser: {msIdCurrentUser.ToString()} exportOnly: {exportOnly})");

            return newAction;
        }

        public async Task<Data.Models.Action> AddImportAction(int key, int targetEnvironmentId, Guid msIdCurrentUser)
        {
            logger.LogDebug($"Begin: SolutionService AddImportAction(key: {key}, targetEnvironmentId: {targetEnvironmentId},  msIdCurrentUser: {msIdCurrentUser.ToString()})");

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

            logger.LogDebug($"End: SolutionService AddImportAction(key: {key}, targetEnvironmentId: {targetEnvironmentId},  msIdCurrentUser: {msIdCurrentUser.ToString()})");

            return newAction;
        }

        public async Task<Data.Models.Action> AddApplyUpgradeAction(int solutionId, int targetEnvironmentId, Guid msIdCurrentUser)
        {
            logger.LogDebug($"Begin: SolutionService  AddApplyUpgradeAction(solutionId: {solutionId}, targetEnvironmentId: {targetEnvironmentId},  msIdCurrentUser: {msIdCurrentUser.ToString()})");

            Solution solution = dbContext.Solutions.First(e => e.Id == solutionId);
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

            logger.LogDebug($"End: SolutionService  AddApplyUpgradeAction(solutionId: {solutionId}, targetEnvironmentId: {targetEnvironmentId},  msIdCurrentUser: {msIdCurrentUser.ToString()})");

            return newAction;
        }

        public async Task<Data.Models.Action> AddEnableFlowsAction(int solutionId, int targetEnvironmentId, Guid msIdCurrentUser)
        {
            logger.LogDebug($"Begin: SolutionService AddEnableFlowsAction(solutionId: {solutionId}, targetEnvironmentId: {targetEnvironmentId},  msIdCurrentUser: {msIdCurrentUser.ToString()})");

            Solution solution = dbContext.Solutions.First(e => e.Id == solutionId);
            User user = this.dbContext.Users.First(e => e.MsId == msIdCurrentUser);

            await this.CheckImportPermission(user.Id, targetEnvironmentId);
            await this.CheckIsConnectionOwerSet(targetEnvironmentId);

            Data.Models.Action newAction = new Data.Models.Action
            {
                Name = $"{solution.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                TargetEnvironment = targetEnvironmentId,
                Type = 4,
                Status = 1,
                StartTime = DateTime.Now,
                Solution = solution.Id
            };

            dbContext.Add(newAction);
            await dbContext.SaveChangesAsync(msIdCurrentUser: msIdCurrentUser);

            logger.LogDebug($"End: SolutionService AddEnableFlowsAction(return Action Id: {newAction.Id})");

            return newAction;
        }

        public async Task CreateUpgrade(Upgrade upgrade, string version)
        {
            logger.LogDebug($"Begin: SolutionService CreateUpgrade(upgrade Version: {upgrade.Version}, version: {version})");

            Application application = this.dbContext.Applications.FirstOrDefault(e => e.Id == upgrade.Application);
            string lastSolution = version == null ? application.Solutions.OrderByDescending(e => e.CreatedOn).FirstOrDefault()?.Version : version;
            if (upgrade.Version == null)
                upgrade.Version = lastSolution == null ? VersionHelper.GetNextMinorVersion("1.0.0.0") : VersionHelper.GetNextMinorVersion(lastSolution);

            await this.CreateUpgradeInDataverse(application.SolutionUniqueName, application.Name, application.DevelopmentEnvironmentNavigation.BasicUrl, application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId, upgrade);
            upgrade.UrlMakerportal = $"https://make.powerapps.com/environments/{application.DevelopmentEnvironmentNavigation.MsId}/solutions/{upgrade.MsId}";

            logger.LogDebug($"End: SolutionService CreateUpgrade(upgrade Version: {upgrade.Version}, version: {version})");
        }

        public async Task<AsyncJob> StartExportInDataverse(string solutionUniqueName, bool isManaged, string basicUrl, Data.Models.Action action, Guid tenantMsIdCurrentUser, int environmentId, int targetEnvironment)
        {
            logger.LogDebug($"Begin: SolutionService StartExportInDataverse(solutionUniqueName: {solutionUniqueName}, isManaged: {isManaged}, basicUrl: {basicUrl}, action Id: {action.Id}, tenantMsIdCurrentUser: {tenantMsIdCurrentUser.ToString()}, environmentId: {environmentId}, targetEnvironment: {targetEnvironment})");

            ExportSolutionAsyncRequest exportSolutionRequest = new ExportSolutionAsyncRequest
            {
                Managed = isManaged,
                SolutionName = solutionUniqueName
            };

            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                try
                {
                    ExportSolutionAsyncResponse response = (ExportSolutionAsyncResponse)await dataverseClient.ExecuteAsync(exportSolutionRequest);
                    AsyncJob asyncJob = new AsyncJob
                    {
                        AsyncOperationId = response.AsyncOperationId,
                        JobId = response.ExportJobId,
                        IsManaged = isManaged,
                        Action = action.Id
                    };
                    logger.LogDebug($"End: SolutionService StartExportInDataverse(solutionUniqueName: {solutionUniqueName}, isManaged: {isManaged}, basicUrl: {basicUrl}, action Id: {action.Id}, tenantMsIdCurrentUser: {tenantMsIdCurrentUser.ToString()}, environmentId: {environmentId}, targetEnvironment: {targetEnvironment})");

                    return asyncJob;
                }
                catch (Exception e)
                {
                    logger.LogError($"Error: SolutionService StartExportInDataverse() Exception: {e}");

                    throw new Exception("Could not start Export in Dataverse");
                }
            }
        }

        public async Task<AsyncJob> StartImportInDataverse(byte[] solutionFileData, Action action, bool asHolding)
        {
            logger.LogDebug($"Begin: SolutionService StartHoldingImportInDataverse(solutionFileData Count: {solutionFileData.Count()}, action BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

            (EntityCollection solutionComponentParameters, string deploymentDetails) = await this.GetSolutionComponentsForImport(action.TargetEnvironment, action.SolutionNavigation.Application);

            ImportSolutionAsyncRequest importSolutionAsyncRequest = new ImportSolutionAsyncRequest
            {
                CustomizationFile = solutionFileData,
                OverwriteUnmanagedCustomizations = action.SolutionNavigation.OverwriteUnmanagedCustomizations ?? true,
                PublishWorkflows = action.SolutionNavigation.EnableWorkflows ?? true,
                HoldingSolution = asHolding,
                ComponentParameters = solutionComponentParameters,
            };

            using (var dataverseClient = new ServiceClient(new Uri(action.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                ImportSolutionAsyncResponse response = (ImportSolutionAsyncResponse)await dataverseClient.ExecuteAsync(importSolutionAsyncRequest);
                action.DeploymentDetails = deploymentDetails;

                AsyncJob asyncJob = new AsyncJob
                {
                    AsyncOperationId = response.AsyncOperationId,
                    JobId = Guid.Parse(response.ImportJobKey),
                    IsManaged = !action.TargetEnvironmentNavigation.DeployUnmanaged,
                    Action = action.Id
                };
                logger.LogDebug($"End: SolutionService StartHoldingImportInDataverse(solutionFileData Count: {solutionFileData.Count()}, action BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

                return asyncJob;
            }
        }

        public async Task<AsyncJob> StartImportAndUpgradeInDataverse(byte[] solutionFileData, Action action)
        {
            logger.LogDebug($"Begin: SolutionService StartImportInDataverse(solutionFileData Count: {solutionFileData.Count()}, action BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

            (EntityCollection solutionComponentParameters, string deploymentDetails) = await this.GetSolutionComponentsForImport(action.TargetEnvironment, action.SolutionNavigation.Application);

            StageAndUpgradeAsyncRequest stageAndUpgradeRequest = new StageAndUpgradeAsyncRequest{
                CustomizationFile = solutionFileData,
                OverwriteUnmanagedCustomizations = action.SolutionNavigation.OverwriteUnmanagedCustomizations ?? true,
                PublishWorkflows = action.SolutionNavigation.EnableWorkflows ?? true,
                ComponentParameters = solutionComponentParameters,
            };

            using (var dataverseClient = new ServiceClient(new Uri(action.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                StageAndUpgradeAsyncResponse response = (StageAndUpgradeAsyncResponse)await dataverseClient.ExecuteAsync(stageAndUpgradeRequest);
                action.DeploymentDetails = deploymentDetails;

                AsyncJob asyncJob = new AsyncJob
                {
                    AsyncOperationId = response.AsyncOperationId,
                    JobId = Guid.Parse(response.ImportJobKey),
                    IsManaged = !action.TargetEnvironmentNavigation.DeployUnmanaged,
                    Action = action.Id
                };
                logger.LogDebug($"End: SolutionService StartImportInDataverse(solutionFileData Count: {solutionFileData.Count()}, action BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

                return asyncJob;
            }
        }

        public async Task<AsyncJob> DeleteAndPromoteInDataverse(Action action)
        {
            logger.LogDebug($"Begin: SolutionService DeleteAndPromoteInDataverse(action TargetEnvironmentNavigation BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

            DeleteAndPromoteRequest deleteAndPromoteRequest = new DeleteAndPromoteRequest
            {
                UniqueName = action.SolutionNavigation.UniqueName
            };

            using (var dataverseClient = new ServiceClient(new Uri(action.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                try
                {
                    DeleteAndPromoteResponse response = (DeleteAndPromoteResponse)await dataverseClient.ExecuteAsync(deleteAndPromoteRequest);
                    return null;
                }
                catch (TimeoutException)
                {
                    Guid solutionHistoryId = await this.solutionHistoryService.GetIdForDeleteAndPromote(action.SolutionNavigation, action.TargetEnvironmentNavigation.BasicUrl);

                    AsyncJob asyncJob = new AsyncJob
                    {
                        JobId = solutionHistoryId,
                        IsManaged = true,
                        Action = action.Id
                    };
                    logger.LogDebug($"Begin: SolutionService DeleteAndPromoteInDataverse(action TargetEnvironmentNavigation BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

                    return asyncJob;
                }
            }
        }

        public async Task<string> DownloadSolutionFileFromDataverse(AsyncJob asyncJob)
        {
            logger.LogDebug($"Begin: SolutionService DownloadSolutionFileFromDatavers(asyncJob BasicUrl: {asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl})");

            DownloadSolutionExportDataRequest downloadSolutionExportDataRequest = new DownloadSolutionExportDataRequest
            {
                ExportJobId = (Guid)asyncJob.JobId
            };

            using (var dataverseClient = new ServiceClient(new Uri(asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                DownloadSolutionExportDataResponse response = (DownloadSolutionExportDataResponse)await dataverseClient.ExecuteAsync(downloadSolutionExportDataRequest);
                string base64 = Convert.ToBase64String(response.ExportSolutionFile);

                logger.LogDebug($"End: SolutionService DownloadSolutionFileFromDatavers(asyncJob BasicUrl: {asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl})");

                return base64;
            }
        }

        public async Task<bool> ExistsSolutionInTargetEnvironment(string solutionUniqueName, string basicUrl, string excludeVersion = "")
        {
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                var query = new QueryExpression("solution"){
                    ColumnSet = new ColumnSet("uniquename", "version"),
                };
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

                if(!String.IsNullOrEmpty(excludeVersion))
                    query.Criteria.AddCondition("version", ConditionOperator.NotEqual, excludeVersion);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                return response.Entities.Count > 0;
            }
        }

        public  async Task<Guid> GetSolutionIdByUniqueName(string solutionUniqueName, string basicUrl){
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                var query = new QueryExpression("solution"){
                    ColumnSet = new ColumnSet("solutionid"),
                    PageInfo = new PagingInfo(){
                        Count = 1,
                        PageNumber = 1 
                    }
                };
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                if(response.Entities.Count == 0)
                    return Guid.Empty;

                var solutionId = (Guid)response.Entities.First()["solutionid"];
                return solutionId;
            }
        }

        private async Task CheckImportPermission(int userId, int environmentId)
        {
            logger.LogDebug($"Begin: SolutionService CheckImportPermission(userId: {userId}, environmentId: {environmentId})");

            UserEnvironment userEnvironment = await this.dbContext.UserEnvironments.FindAsync(userId, environmentId);
            if (userEnvironment == null)
                throw new Exception("User does not have permission within PowerCID Portal to import/apply upgrade on target environment. Your administrator can assign the permission via Power CID Portal user management.");

            logger.LogDebug($"End: SolutionService CheckImportPermission(userId: {userId}, environmentId: {environmentId})");
        }

        private async Task CheckIsConnectionOwerSet(int environmentId)
        {
            Data.Models.Environment environment = await this.dbContext.Environments.FindAsync(environmentId);
            if (String.IsNullOrEmpty(environment.ConnectionsOwner))
                throw new Exception("Connection Owner is not set for target environment.");
        }

        private async Task CreateUpgradeInDataverse(string solutionUniqueName, string solutionDisplayName, string basicUrl, Guid tenantMsId, Upgrade upgrade)
        {
            logger.LogDebug($"Begin: SolutionService CreateUpgradeInDataverse(solutionUniqueName: {solutionUniqueName}, solutionDisplayName: {solutionDisplayName}, basicUrl: {basicUrl}, tenantMsId: {tenantMsId.ToString()}, upgrade Version: {upgrade.Version})");

            CloneAsSolutionRequest cloneAsSolutionRequest = new CloneAsSolutionRequest
            {

                DisplayName = solutionDisplayName,
                ParentSolutionUniqueName = solutionUniqueName,
                VersionNumber = upgrade.Version
            };

            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                try
                {
                    CloneAsSolutionResponse response = (CloneAsSolutionResponse)await dataverseClient.ExecuteAsync(cloneAsSolutionRequest);
                    upgrade.MsId = response.SolutionId;
                    upgrade.UniqueName = solutionUniqueName;
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not create Upgrade in Dataverse: {e.Message}");
                }
            }
            logger.LogDebug($"End: SolutionService CreateUpgradeInDataverse(solutionUniqueName: {solutionUniqueName}, solutionDisplayName: {solutionDisplayName}, basicUrl: {basicUrl}, tenantMsId: {tenantMsId.ToString()}, upgrade Version: {upgrade.Version})");
        }

        private async Task<(EntityCollection, string)> GetSolutionComponentsForImport(int environmentId, int applicationId)
        {
            logger.LogDebug($"Begin: SolutionService GetSolutionComponentsForImport(environmentId: {environmentId}, applicationId: {applicationId})");

            EntityCollection solutionComponentParameters = new EntityCollection();

            (var connectionReferenceEntities, string deploymentDetailsConnectionReferences) = await this.GetConnectionReferencesForImport(environmentId, applicationId);
            (var environmentVariableEntities, string deploymentDetailsEnvironmentVariables) = await this.GetEnvironmentVariablesForImport(environmentId, applicationId);
            solutionComponentParameters.Entities.AddRange(connectionReferenceEntities.Entities);
            solutionComponentParameters.Entities.AddRange(environmentVariableEntities.Entities);

            if (solutionComponentParameters.Entities.Count == 0)
                return (null, null);

            string deploymentDetails = $"{deploymentDetailsConnectionReferences}\n{deploymentDetailsEnvironmentVariables}";

            logger.LogDebug($"End: SolutionService GetSolutionComponentsForImport(environmentId: {environmentId}, applicationId: {applicationId})");

            return (solutionComponentParameters, deploymentDetails);
        }

        private async Task<(EntityCollection, string)> GetEnvironmentVariablesForImport(int environmentId, int applicationId)
        {

            logger.LogDebug($"Begin: SolutionService  GetEnvironmentVariablesForImport(environmentId: {environmentId}, applicationId: {applicationId})");

            await this.environmentVariableService.CleanEnvironmentVariables(applicationId);

            EntityCollection environmentVariableEntities = new EntityCollection();
            string deploymentDetails = "";

            var environmentVariableEnvironments = this.dbContext.EnvironmentVariableEnvironments.Where(e => e.Environment == environmentId && e.EnvironmentVariableNavigation.Application == applicationId).ToList();

            if(environmentVariableEnvironments.Count > 0)
                deploymentDetails += "Used Environment Variables:\n";

            foreach (EnvironmentVariableEnvironment environmentVariableEnvironment in environmentVariableEnvironments)
            {
                Entity connRecord = new Entity("environmentvariablevalue");
                connRecord.Attributes.Add("schemaname", environmentVariableEnvironment.EnvironmentVariableNavigation.LogicalName);
                connRecord.Attributes.Add("value", environmentVariableEnvironment.Value);
                environmentVariableEntities.Entities.Add(connRecord);

                deploymentDetails += $"-{environmentVariableEnvironment.EnvironmentVariableNavigation.LogicalName}: {environmentVariableEnvironment.Value}\n";
            }
            logger.LogDebug($"End: SolutionService  GetEnvironmentVariablesForImport(environmentId: {environmentId}, applicationId: {applicationId})");

            return (environmentVariableEntities, deploymentDetails);
        }

        private async Task<(EntityCollection, string)> GetConnectionReferencesForImport(int environmentId, int applicationId)
        {
            logger.LogDebug($"Begin: SolutionService  GetConnectionReferencesForImport(environmentId: {environmentId}, applicationId: {applicationId})");

            await this.connectionReferenceService.CleanConnectionReferences(applicationId);

            EntityCollection connectionReferenceEntities = new EntityCollection();
            string deploymentDetails = "";

            var connectionReferenceEnvironments = this.dbContext.ConnectionReferenceEnvironments.Where(e => e.Environment == environmentId && e.ConnectionReferenceNavigation.Application == applicationId).ToList();

            if(connectionReferenceEnvironments.Count > 0)
                deploymentDetails += "Used Connection References:\n";

            foreach (ConnectionReferenceEnvironment connectionReferenceEnvironment in connectionReferenceEnvironments)
            {
                Entity connRecord = new Entity("connectionreference");
                connRecord.Attributes.Add("connectionreferencedisplayname", connectionReferenceEnvironment.ConnectionReferenceNavigation.DisplayName);
                connRecord.Attributes.Add("connectionreferencelogicalname", connectionReferenceEnvironment.ConnectionReferenceNavigation.LogicalName);
                connRecord.Attributes.Add("connectorid", connectionReferenceEnvironment.ConnectionReferenceNavigation.ConnectorId);
                connRecord.Attributes.Add("connectionid", connectionReferenceEnvironment.ConnectionId);
                connectionReferenceEntities.Entities.Add(connRecord);

                deploymentDetails += $"-{connectionReferenceEnvironment.ConnectionReferenceNavigation.LogicalName}: {connectionReferenceEnvironment.ConnectionId}\n";
            }
            logger.LogDebug($"End: SolutionService  GetConnectionReferencesForImport(environmentId: {environmentId}, applicationId: {applicationId})");

            return (connectionReferenceEntities, deploymentDetails);
        }
    }
}