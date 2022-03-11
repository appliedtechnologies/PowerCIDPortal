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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace at.D365.PowerCID.Portal.Services
{
    public class SolutionService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly IDownstreamWebApi downstreamWebApi;
        private readonly GitHubService gitHubService;
        private readonly ConnectionReferenceService connectionReferenceService;
        private readonly EnvironmentVariableService environmentVariableService;
        private readonly IConfiguration configuration;
        public SolutionService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            var scope = serviceProvider.CreateScope();
            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.downstreamWebApi = scope.ServiceProvider.GetRequiredService<IDownstreamWebApi>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.gitHubService = scope.ServiceProvider.GetRequiredService<GitHubService>();
            this.connectionReferenceService = scope.ServiceProvider.GetRequiredService<ConnectionReferenceService>();
            this.environmentVariableService = scope.ServiceProvider.GetRequiredService<EnvironmentVariableService>();
        }

        public async Task<Data.Models.Action> Export(int key, Guid tenantMsIdCurrentUser, Guid msIdCurrentUser, bool exportOnly, int targetEnvironmentForImport = 0)
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
                Status = 2,
                StartTime = DateTime.Now,
                Solution = solution.Id,
                ExportOnly = exportOnly
            };

            dbContext.Add(newAction);

            var asyncJobManaged = await this.StartExportInDataverse(solution.UniqueName, true, solution.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, newAction, tenantMsIdCurrentUser, solution.ApplicationNavigation.DevelopmentEnvironment, targetEnvironmentForImport);
            var asyncJobUnmanaged = await this.StartExportInDataverse(solution.UniqueName, false, solution.ApplicationNavigation.DevelopmentEnvironmentNavigation.BasicUrl, newAction, tenantMsIdCurrentUser, solution.ApplicationNavigation.DevelopmentEnvironment, targetEnvironmentForImport);


            asyncJobManaged.ActionNavigation = newAction;
            asyncJobUnmanaged.ActionNavigation = newAction;
            asyncJobUnmanaged.ImportTargetEnvironment = null;

            dbContext.Add(asyncJobManaged);

            dbContext.Add(asyncJobUnmanaged);
            await dbContext.SaveChangesAsync(msIdCurrentUser: msIdCurrentUser);
            return newAction;
        }

        public async Task<Data.Models.Action> Import(int key, int targetEnvironmentId, Guid msIdCurrentUser, bool isPatch)
        {

            Solution solution = dbContext.Solutions.First(e => e.Id == key);
            User user = this.dbContext.Users.First(e => e.MsId == msIdCurrentUser);
            Tenant tenant = dbContext.Environments.First(e => e.Id == targetEnvironmentId).TenantNavigation;
            byte[] exportSolutionFile = await GetSolutionFromGitHub(tenant, solution);
            string basicUrl = dbContext.Environments.First(e => e.Id == targetEnvironmentId).BasicUrl;

            await this.CheckImportPermission(user.Id, targetEnvironmentId);

            Data.Models.Action newAction = new Data.Models.Action
            {
                Name = $"{solution.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                TargetEnvironment = targetEnvironmentId,
                Type = 2,
                Status = 2,
                StartTime = DateTime.Now,
                Solution = solution.Id
            };

            dbContext.Add(newAction);

            AsyncJob asyncJobManaged = await this.StartImportInDataverse(exportSolutionFile, basicUrl, newAction, tenant.MsId.ToString(), targetEnvironmentId, isPatch);

            asyncJobManaged.ActionNavigation = newAction;

            dbContext.Add(asyncJobManaged);
            await dbContext.SaveChangesAsync(msIdCurrentUser: msIdCurrentUser);

            return newAction;
        }

        public async Task CreateUpgrade(Upgrade upgrade, string version)
        {
            Application application = this.dbContext.Applications.FirstOrDefault(e => e.Id == upgrade.Application);
            string lastSolution = version == null ? application.Solutions.OrderByDescending(e => e.CreatedOn).FirstOrDefault()?.Version : version;
            upgrade.Version = lastSolution == null ? VersionHelper.GetNextMinorVersion("1.0.0.0") : VersionHelper.GetNextMinorVersion(lastSolution);

            await this.CreateUpgradeInDataverse(application.SolutionUniqueName, application.Name, application.DevelopmentEnvironmentNavigation.BasicUrl, application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId, upgrade);
            upgrade.UrlMakerportal = $"https://make.powerapps.com/environments/{application.DevelopmentEnvironmentNavigation.MsId}/solutions/{upgrade.MsId}";
        }

        public async Task<byte[]> GetSolutionFromGitHub(Tenant tenant, Solution solution)
        {
            (var installation, var installationClient) = await this.gitHubService.GetInstallationWithClient(tenant.GitHubInstallationId);

            string path = $"applications/{ solution.ApplicationNavigation.Id }_{ solution.ApplicationNavigation.SolutionUniqueName }/{ solution.Version }/{solution.Name}_managed.zip";
            string[] gitHubRepositoryName = tenant.GitHubRepositoryName.Split('/');
            string repositoryName = gitHubRepositoryName[1];
            string owner = gitHubRepositoryName[0];

            byte[] solutionZipFile = await installationClient.Repository.Content.GetRawContent(owner, repositoryName, path);
            return solutionZipFile;
        }

        public async Task<string> GetSolutionFromGitHubAsBase64String(Tenant tenant, Solution solution)
        {
            var solutionZipFile = await this.GetSolutionFromGitHub(tenant, solution);
            return Convert.ToBase64String(solutionZipFile);
        }

        private async Task CreateUpgradeInDataverse(string solutionUniqueName, string solutionDisplayName, string basicUrl, Guid tenantMsId, Upgrade upgrade)
        {
            JObject newSolution = new JObject();
            newSolution.Add("DisplayName", solutionDisplayName);
            newSolution.Add("ParentSolutionUniqueName", solutionUniqueName);
            newSolution.Add("VersionNumber", upgrade.Version);

            StringContent solutionContent = new StringContent(JsonConvert.SerializeObject(newSolution), Encoding.UTF8, mediaType: "application/json");

            string apiUri = basicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + "/CloneAsSolution";
            string configAuthority = configuration["AzureAd:Instance"] + tenantMsId;
            string[] scope = new string[] { basicUrl + "/.default" };
            string token = await GetToken(configAuthority, scope);
            HttpResponseMessage response = await HttpPostRequest(apiUri, token, solutionContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Could not create Upgrade in Dataverse: {response.ReasonPhrase}");
            }
            else
            {
                upgrade.MsId = (Guid)(await response.Content.ReadAsAsync<JObject>())["SolutionId"];
                upgrade.UniqueName = solutionUniqueName;
            }
        }

        private async Task<AsyncJob> StartExportInDataverse(string solutionUniqueName, bool isManaged, string basicUrl, Data.Models.Action action, Guid tenantMsIdCurrentUser, int environmentId, int targetEnvironment)
        {
            // Payload
            JObject payload = new JObject();
            payload.Add("Managed", isManaged);
            payload.Add("SolutionName", solutionUniqueName);
            StringContent payloadJson = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, mediaType: "application/json");

            // Http Request
            string configAuthority = configuration["AzureAd:Instance"] + tenantMsIdCurrentUser;
            string[] scope = new string[] { dbContext.Environments.FirstOrDefault(e => e.Id == environmentId).BasicUrl + "/.default" };
            string token = await GetToken(configAuthority, scope);
            HttpResponseMessage response = await HttpPostRequest(basicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + "/ExportSolutionAsync", token, payloadJson);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not start Export in Dataverse");
            }
            else
            {
                JObject reponseData = await response.Content.ReadAsAsync<JObject>();
                AsyncJob asyncJob = new AsyncJob
                {
                    AsyncOperationId = (Guid)reponseData["AsyncOperationId"],
                    JobId = (Guid)reponseData["ExportJobId"],
                    IsManaged = isManaged,
                    ImportTargetEnvironment = targetEnvironment == 0 ? null : targetEnvironment
                };

                return asyncJob;
            }
        }

        private async Task CheckImportPermission(int userId, int environmentId)
        {
            UserEnvironment userEnvironment = await this.dbContext.UserEnvironments.FindAsync(userId, environmentId);
            if (userEnvironment == null)
                throw new Exception("User does not have permission to import on target environment.");
        }

        private async Task<AsyncJob> StartImportInDataverse(byte[] solutionFileData, string basicUrl, Data.Models.Action action, string tenantMsId, int environmentId, bool isPatch)
        {
            Upgrade upgrade;
            upgrade = isPatch == false ? (Upgrade)action.SolutionNavigation : default;
            bool existsSolutionInTargetEnvironment = ExistsSolutionInTargetEnvironment(action.SolutionNavigation.Application, environmentId);

            EntityCollection solutionComponentParameters = await this.GetSolutionComponentsForImport(environmentId, action.SolutionNavigation.Application);

            ImportSolutionAsyncRequest importSolutionAsyncRequest = new ImportSolutionAsyncRequest{
                CustomizationFile = solutionFileData,
                OverwriteUnmanagedCustomizations = true,
                PublishWorkflows = true,
                HoldingSolution = !isPatch && existsSolutionInTargetEnvironment == true,
                ComponentParameters = solutionComponentParameters
            };

            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                ImportSolutionAsyncResponse response = (ImportSolutionAsyncResponse)dataverseClient.Execute(importSolutionAsyncRequest);

                AsyncJob asyncJob = new AsyncJob
                {
                    AsyncOperationId = response.AsyncOperationId,
                    JobId = Guid.Parse(response.ImportJobKey),
                    IsManaged = true
                };

                return asyncJob;
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

        private bool ExistsSolutionInTargetEnvironment(int applicationId, int targetEnvironmentId)
        {
            return dbContext.Actions.Any(x => x.SolutionNavigation.Application == applicationId && x.TargetEnvironment == targetEnvironmentId && x.Result == 1);
        }

        #region httpMethoden
        private async Task<HttpResponseMessage> HttpGetRequest(string apiUri, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Call the web API.
            HttpResponseMessage response = await httpClient.GetAsync(apiUri);

            return response;
        }

        private async Task<HttpResponseMessage> HttpPostRequest(string apiUri, string token, StringContent content)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Call the web API.
            HttpResponseMessage response = await httpClient.PostAsync(apiUri, content);

            return response;
        }

        private async Task<string> GetToken(string configAuthority, string[] scopes)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(configuration["AzureAd:ClientId"])
                .WithClientSecret(configuration["AzureAd:ClientSecret"])
                .WithAuthority(new Uri(configAuthority))
                .Build();


            AuthenticationResult result = null;
            try
            {
                result = await app.AcquireTokenForClient(scopes)
                                 .ExecuteAsync();
            }
            catch (MsalUiRequiredException e)
            {
                throw new Exception(e.Message);
                // The application doesn't have sufficient permissions.
                // - Did you declare enough app permissions during app creation?
                // - Did the tenant admin grant permissions to the application?
            }
            catch (MsalServiceException e) when (e.Message.Contains("AADSTS70011"))
            {
                throw new Exception(e.Message);
                // Invalid scope. The scope has to be in the form "https://resourceurl/.default"
                // Mitigation: Change the scope to be as expected.
            }

            return result.AccessToken;
        }
        #endregion
    }
}