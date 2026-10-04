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
using Action = at.D365.PowerCID.Portal.Data.Models.Action;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class SolutionService
    {
        private readonly ILogger logger;
        private readonly atPowerCIDContext dbContext;
        private readonly DeploymentSettingsService deploymentSettingsService;
        private readonly SolutionHistoryService solutionHistoryService;
        private readonly IConfiguration configuration;

        public SolutionService(ILogger<SolutionService> logger, atPowerCIDContext dbContext, DeploymentSettingsService deploymentSettingsService, SolutionHistoryService solutionHistoryService, IConfiguration configuration)
        {
            this.logger = logger;

            this.dbContext = dbContext;
            this.configuration = configuration;
            this.deploymentSettingsService = deploymentSettingsService;
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
            await this.deploymentSettingsService.EnsureReadyForImport(solution.Id);

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

            await this.deploymentSettingsService.EnsureReadyForImport(solution.Id);
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
            await deploymentSettingsService.CreateActionSnapshot(newAction.Id, solution.Id, targetEnvironmentId);

            logger.LogDebug($"End: SolutionService AddImportAction(key: {key}, targetEnvironmentId: {targetEnvironmentId},  msIdCurrentUser: {msIdCurrentUser.ToString()})");

            return newAction;
        }

        public async Task<Data.Models.Action> AddExternalImportAction(int key, int externalEnvironmentId, int externalDeploymentPathId, Guid msIdCurrentUser)
        {
            logger.LogDebug($"Begin: SolutionService AddExternalImportAction(key: {key}, externalEnvironmentId: {externalEnvironmentId}, externalDeploymentPathId: {externalDeploymentPathId}, msIdCurrentUser: {msIdCurrentUser.ToString()})");

            Solution solution = dbContext.Solutions.First(e => e.Id == key);
            User user = this.dbContext.Users.First(e => e.MsId == msIdCurrentUser);

            await this.deploymentSettingsService.EnsureReadyForImport(solution.Id);
            ExternalEnvironment externalEnvironment = await this.CheckExternalImportPermission(user.Id, solution, externalEnvironmentId, externalDeploymentPathId);

            Data.Models.Action newAction = new Data.Models.Action
            {
                Name = $"{solution.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                TargetEnvironment = externalEnvironment.Environment,
                Type = 2,
                Status = 1,
                StartTime = DateTime.Now,
                Solution = solution.Id,
                IsExternalDelivery = true
            };

            dbContext.Add(newAction);
            await dbContext.SaveChangesAsync(msIdCurrentUser: msIdCurrentUser);
            await deploymentSettingsService.CreateActionSnapshot(newAction.Id, solution.Id, externalEnvironment.Environment);

            logger.LogDebug($"End: SolutionService AddExternalImportAction(key: {key}, externalEnvironmentId: {externalEnvironmentId}, externalDeploymentPathId: {externalDeploymentPathId}, msIdCurrentUser: {msIdCurrentUser.ToString()})");

            return newAction;
        }

        public async Task<Data.Models.Action> AddApplyUpgradeAction(int solutionId, int targetEnvironmentId, Guid msIdCurrentUser)
        {
            logger.LogDebug($"Begin: SolutionService  AddApplyUpgradeAction(solutionId: {solutionId}, targetEnvironmentId: {targetEnvironmentId},  msIdCurrentUser: {msIdCurrentUser.ToString()})");

            Solution solution = dbContext.Solutions.First(e => e.Id == solutionId);
            User user = this.dbContext.Users.First(e => e.MsId == msIdCurrentUser);

            await this.deploymentSettingsService.EnsureReadyForImport(solution.Id);
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
            await deploymentSettingsService.CreateActionSnapshot(newAction.Id, solution.Id, targetEnvironmentId);

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

            Application application = this.dbContext.Applications.First(e => e.Id == upgrade.Application);

            if (upgrade.Version == null)
            {
                if (!String.IsNullOrEmpty(version))
                    upgrade.Version = version;
                else
                {
                    string lastVersionFromDataverse = await GetCurrentSolutionVersion(application.SolutionUniqueName, application.DevelopmentEnvironmentNavigation.BasicUrl);
                    if (!String.IsNullOrEmpty(lastVersionFromDataverse))
                        upgrade.Version = VersionHelper.GetNextMinorVersion(lastVersionFromDataverse);
                    else
                    {
                        string lastSolution = application.Solutions.OrderByDescending(e => e.CreatedOn).FirstOrDefault()?.Version;
                        upgrade.Version = lastSolution == null ? VersionHelper.GetNextMinorVersion("1.0.0.0") : VersionHelper.GetNextMinorVersion(lastSolution);
                    }
                }
            }

            await this.CreateUpgradeInDataverse(application.SolutionUniqueName, application.Name, application.DevelopmentEnvironmentNavigation.BasicUrl, application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId, upgrade);
            upgrade.UrlMakerportal = $"https://make.powerapps.com/environments/{application.DevelopmentEnvironmentNavigation.MsId}/solutions/{upgrade.MsId}";

            logger.LogDebug($"End: SolutionService CreateUpgrade(upgrade Version: {upgrade.Version}, version: {version})");
        }

        public Task InitializeDeploymentManifest(int solutionId)
        {
            return deploymentSettingsService.InitializeManifestAndInherit(solutionId);
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

        public async Task<AsyncJob> StartImportInDataverse(byte[] solutionFileData, Action action, bool asHolding, bool unmanaged)
        {
            logger.LogDebug($"Begin: SolutionService StartHoldingImportInDataverse(solutionFileData Count: {solutionFileData.Count()}, action BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

            (EntityCollection solutionComponentParameters, string deploymentDetails) = await this.GetSolutionComponentsForImport(action.Id);

            if (unmanaged)
                deploymentDetails = "unmanaged deployment \n\n" + deploymentDetails;
            else
                deploymentDetails = "managed deployment \n\n" + deploymentDetails;

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
                    IsManaged = !unmanaged,
                    Action = action.Id
                };
                logger.LogDebug($"End: SolutionService StartHoldingImportInDataverse(solutionFileData Count: {solutionFileData.Count()}, action BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

                return asyncJob;
            }
        }

        public async Task<AsyncJob> StartImportAndUpgradeInDataverse(byte[] solutionFileData, Action action, bool unmanaged)
        {
            logger.LogDebug($"Begin: SolutionService StartImportInDataverse(solutionFileData Count: {solutionFileData.Count()}, action BasicUrl: {action.TargetEnvironmentNavigation.BasicUrl})");

            (EntityCollection solutionComponentParameters, string deploymentDetails) = await this.GetSolutionComponentsForImport(action.Id);

            if (unmanaged)
                deploymentDetails = "unmanaged deployment \n\n" + deploymentDetails;
            else
                deploymentDetails = "managed deployment \n\n" + deploymentDetails;

            StageAndUpgradeAsyncRequest stageAndUpgradeRequest = new StageAndUpgradeAsyncRequest
            {
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
                    IsManaged = !unmanaged,
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
            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("uniquename", "version"),
                };
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

                if (!String.IsNullOrEmpty(excludeVersion))
                    query.Criteria.AddCondition("version", ConditionOperator.NotEqual, excludeVersion);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                return response.Entities.Count > 0;
            }
        }

        public async Task<Guid> GetSolutionIdByUniqueName(string solutionUniqueName, string basicUrl)
        {
            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("solutionid"),
                    PageInfo = new PagingInfo()
                    {
                        Count = 1,
                        PageNumber = 1
                    }
                };
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                if (response.Entities.Count == 0)
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

        private async Task<ExternalEnvironment> CheckExternalImportPermission(int userId, Solution solution, int externalEnvironmentId, int externalDeploymentPathId)
        {
            logger.LogDebug($"Begin: SolutionService CheckExternalImportPermission(userId: {userId}, solution Id: {solution.Id}, externalEnvironmentId: {externalEnvironmentId}, externalDeploymentPathId: {externalDeploymentPathId})");

            if (!solution.IsReleasedExternally)
                throw new Exception("This solution version is not released for external deployment.");

            ExternalEnvironment externalEnvironment = await this.dbContext.ExternalEnvironments.FirstOrDefaultAsync(e => e.Id == externalEnvironmentId);
            if (externalEnvironment == null)
                throw new Exception("The external environment does not exist.");

            ExternalDeploymentPathEnvironment step = await this.dbContext.ExternalDeploymentPathEnvironments.FirstOrDefaultAsync(e => e.ExternalDeploymentPath == externalDeploymentPathId && e.ExternalEnvironment == externalEnvironmentId);
            if (step == null)
                throw new Exception("The external environment is not part of the selected external deployment path.");

            Guid targetTenantMsId = await this.dbContext.Environments
                .Where(e => e.Id == externalEnvironment.Environment)
                .Select(e => e.TenantNavigation.MsId)
                .FirstAsync();

            bool hasPermission = await this.dbContext.UserExternalTenants.AnyAsync(x => x.User == userId && x.TenantNavigation.MsId == targetTenantMsId);
            if (!hasPermission)
                throw new Exception("User does not have permission within PowerCID Portal to deploy into this external tenant. Your administrator can assign the permission via Power CID Portal user management.");

            if (step.StepNumber > 1)
            {
                int previousExternalEnvironmentId = (await this.dbContext.ExternalDeploymentPathEnvironments.FirstAsync(x => x.ExternalDeploymentPath == externalDeploymentPathId && x.StepNumber == step.StepNumber - 1)).ExternalEnvironment;
                int previousEnvironmentId = (await this.dbContext.ExternalEnvironments.FirstAsync(e => e.Id == previousExternalEnvironmentId)).Environment;

                bool previousStepSucceeded = this.dbContext.Actions.Any(x => x.Solution == solution.Id && x.TargetEnvironment == previousEnvironmentId && x.Result == 1 && x.IsExternalDelivery);
                if (!previousStepSucceeded)
                    throw new Exception("Can't skip a previous external deployment environment.");
            }

            logger.LogDebug($"End: SolutionService CheckExternalImportPermission(userId: {userId}, solution Id: {solution.Id}, externalEnvironmentId: {externalEnvironmentId}, externalDeploymentPathId: {externalDeploymentPathId})");

            return externalEnvironment;
        }

        private async Task CheckIsConnectionOwerSet(int environmentId)
        {
            Data.Models.Environment environment = await this.dbContext.Environments.FindAsync(environmentId);
            if (String.IsNullOrEmpty(environment.ConnectionsOwner))
                throw new Exception("Connection Owner is not set for target environment.");
        }

        private async Task<string> GetCurrentSolutionVersion(string solutionUniqueName, string basicUrl)
        {
            logger.LogDebug($"Begin: SolutionService GetCurrentSolutionVersion(solutionUniqueName: {solutionUniqueName}, basicUrl: {basicUrl})");

            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                var query = new QueryExpression("solution")
                {
                    ColumnSet = new ColumnSet("uniquename", "version"),
                    PageInfo = new PagingInfo()
                    {
                        Count = 1,
                        PageNumber = 1
                    }
                };
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                if (response.Entities.Count == 0)
                    return null;

                var version = (string)response.Entities.First()["version"];
                return version;
            }
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

        private async Task<(EntityCollection, string)> GetSolutionComponentsForImport(int actionId)
        {
            logger.LogDebug($"Begin: SolutionService GetSolutionComponentsForImport(actionId: {actionId})");

            var settings = await deploymentSettingsService.GetSettingsForAction(actionId);
            var solutionComponentParameters = new EntityCollection();
            var connectionReferenceCount = 0;
            var environmentVariableCount = 0;

            foreach (var setting in settings)
            {
                if (setting.Kind == DeploymentSettingKind.ConnectionReference)
                {
                    var connectionReference = new Entity("connectionreference");
                    connectionReference.Attributes.Add("connectionreferencedisplayname", setting.DisplayName);
                    connectionReference.Attributes.Add("connectionreferencelogicalname", setting.LogicalName);
                    connectionReference.Attributes.Add("connectorid", setting.ConnectorId);
                    connectionReference.Attributes.Add("connectionid", setting.Value);
                    solutionComponentParameters.Entities.Add(connectionReference);
                    connectionReferenceCount++;
                }
                else
                {
                    var environmentVariable = new Entity("environmentvariablevalue");
                    environmentVariable.Attributes.Add("schemaname", setting.LogicalName);
                    environmentVariable.Attributes.Add("value", setting.Value);
                    solutionComponentParameters.Entities.Add(environmentVariable);
                    environmentVariableCount++;
                }
            }

            var deploymentDetails =
                $"Deployment settings: {connectionReferenceCount} connection references, {environmentVariableCount} environment variables.";
            logger.LogDebug(
                $"End: SolutionService GetSolutionComponentsForImport(actionId: {actionId}, settingCount: {settings.Count})");
            return (solutionComponentParameters, deploymentDetails);
        }
    }
}