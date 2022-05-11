using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Services
{
    public class AsyncJobService : IHostedService, IDisposable
    {
        private readonly IServiceProvider serviceProvider;
        private readonly atPowerCIDContext dbContext;
        private readonly IDownstreamWebApi downstreamWebApi;
        private readonly IConfiguration configuration;
        private readonly GitHubService gitHubService;
        private readonly SolutionService solutionService;

        private System.Timers.Timer timer;

        public AsyncJobService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            var scope = serviceProvider.CreateScope();

            this.dbContext = scope.ServiceProvider.GetRequiredService<atPowerCIDContext>();
            this.downstreamWebApi = scope.ServiceProvider.GetRequiredService<IDownstreamWebApi>();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.gitHubService = scope.ServiceProvider.GetRequiredService<GitHubService>();
            this.solutionService = scope.ServiceProvider.GetRequiredService<SolutionService>();
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ScheduleJob(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Stop();
            await Task.CompletedTask;
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

        private void UpdateSuccessfulAction(AsyncJob asyncJob)
        {
            asyncJob.ActionNavigation.Status = 3;
            asyncJob.ActionNavigation.Result = 1;
            asyncJob.ActionNavigation.FinishTime = DateTime.Now;
        }

        private void UpdateFailedAction(AsyncJob asyncJob, string errorMessage)
        {
            asyncJob.ActionNavigation.Status = 3;
            asyncJob.ActionNavigation.Result = 2;
            asyncJob.ActionNavigation.FinishTime = DateTime.Now;
            asyncJob.ActionNavigation.ErrorMessage = errorMessage;
        }


        private async Task<string> DownloadSolution(AsyncJob asyncJob, string token, JObject reponseData)
        {
            //ApiUrl
            string apiUri = asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + "/DownloadSolutionExportData";

            //payload
            JObject payload = new JObject();
            payload.Add("ExportJobId", asyncJob.JobId);
            StringContent payloadJson = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, mediaType: "application/json");

            // Request
            var exportResponse = await HttpPostRequest(apiUri, token, payloadJson);

            //Respone
            JObject exportReponseData = await exportResponse.Content.ReadAsAsync<JObject>();
            string exportSolutionFile = (string)exportReponseData["ExportSolutionFile"];

            return exportSolutionFile;
        }


        private void SaveSolutionInGitHub(AsyncJob asyncJob, string exportSolutionFile, GitHubClient installationClient, string owner, string repositoryName)
        {
            string managed = asyncJob.IsManaged == true ? "managed" : "unmanaged";

            try
            {
                // 1. Get the SHA of the latest commit of the main branch.
                var headMasterRef = "heads/main";
                var masterReference = installationClient.Git.Reference.Get(owner, repositoryName, headMasterRef).Result; // Get reference of master branch
                var latestCommit = installationClient.Git.Commit.Get(owner, repositoryName,
                masterReference.Object.Sha).Result; // Get the lastet commit of this branch
                var nt = new NewTree { BaseTree = latestCommit.Tree.Sha };

                //2. Create the blob(s) corresponding to your file(s)
                var textBlob = new NewBlob { Encoding = EncodingType.Base64, Content = exportSolutionFile };
                var textBlobRef = installationClient.Git.Blob.Create(owner, repositoryName, textBlob);

                // 3. Create a new tree with:
                nt.Tree.Add(new NewTreeItem { Path = $"applications/{ asyncJob.ActionNavigation.SolutionNavigation.ApplicationNavigation.Id }_{ asyncJob.ActionNavigation.SolutionNavigation.ApplicationNavigation.SolutionUniqueName }/{ asyncJob.ActionNavigation.SolutionNavigation.Version }/{asyncJob.ActionNavigation.SolutionNavigation.Name}_{managed}.zip", Mode = FileMode.File, Type = TreeType.Blob, Sha = textBlobRef.Result.Sha });
                var newTree = installationClient.Git.Tree.Create(owner, repositoryName, nt).Result;

                // 4. Create the commit with the SHAs of the tree and the reference of master branchS
                // Create Commit
                var newCommit = new NewCommit($"Commit {managed} Export for ActionId {asyncJob.Action} and AsyncJob MsId {asyncJob.JobId}", newTree.Sha, masterReference.Object.Sha);
                var commit = installationClient.Git.Commit.Create(owner, repositoryName, newCommit).Result;

                // 5. Update the reference of master branch with the SHA of the commit
                // Update HEAD with the commit
                installationClient.Git.Reference.Update(owner, repositoryName, headMasterRef, new ReferenceUpdate(commit.Sha));
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {

            var asyncJobsByEnvironment = dbContext.AsyncJobs.ToList().GroupBy(e => e.ActionNavigation.TargetEnvironment);

            if (asyncJobsByEnvironment.Count() > 0)
            {
                foreach (var environmentGroup in asyncJobsByEnvironment)
                {
                    var environmentId = environmentGroup.Key;
                    Tenant tenant = dbContext.Tenants.FirstOrDefault(t => t.Environments.Any(e => e.Id == environmentId));
                    string configAuthority = configuration["AzureAd:Instance"] + tenant.MsId;
                    string[] scope = new string[] { dbContext.Environments.FirstOrDefault(e => e.Id == environmentId).BasicUrl + "/.default" };
                    string token = await GetToken(configAuthority, scope);

                    string[] gitHubRepositoryName = tenant.GitHubRepositoryName.Split('/');
                    string repositoryName = gitHubRepositoryName[1];
                    string owner = gitHubRepositoryName[0];

                    (var installation, var installationClient) = await gitHubService.GetInstallationWithClient(tenant.GitHubInstallationId);

                    foreach (AsyncJob asyncJob in environmentGroup)
                    {
                        // Applying Upgrade Status überprüfen
                        if (asyncJob.AsyncOperationId == null)
                        {
                            JObject responseMessageData = await GetSolutionHistory(asyncJob, token);
                            string endtime = (string)responseMessageData["value"][0]["msdyn_endtime"];

                            if (endtime != null)
                            {
                                int errorCode = (int)responseMessageData["value"][0]["msdyn_errorcode"];

                                if (errorCode != 0)
                                {
                                    string errorMessage = (string)responseMessageData["value"][0]["msdyn_exceptionmessage"];
                                    UpdateFailedAction(asyncJob, errorMessage);
                                }
                                else
                                {
                                    UpdateSuccessfulAction(asyncJob);
                                }
                                dbContext.Remove(asyncJob);
                            }
                        }
                        else
                        {
                            //Async job verarbeiten
                            string apiUri = asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + $"asyncoperations({asyncJob.AsyncOperationId})";
                            HttpResponseMessage response = await HttpGetRequest(apiUri, token);
                            JObject responseData = await response.Content.ReadAsAsync<JObject>();

                            // Completed Success
                            if ((string)responseData["statecode"] == "3" && (string)responseData["statuscode"] == "30")
                            {
                                bool isPatch = asyncJob.ActionNavigation.SolutionNavigation.GetType().Name.Contains("Patch");

                                // Export
                                if (asyncJob.ActionNavigation.Type == 1)
                                {
                                    string exportSolutionFile = await DownloadSolution(asyncJob, token, responseData);
                                    SaveSolutionInGitHub(asyncJob, exportSolutionFile, installationClient, owner, repositoryName);
                                    UpdateSuccessfulAction(asyncJob);
                                    await dbContext.SaveChangesAsync(asyncJob.ActionNavigation.CreatedByNavigation.MsId);

                                    // Export with Import | Start Import
                                    if (asyncJob.ActionNavigation.ExportOnly == false && asyncJob.IsManaged == true)
                                    {
                                        try
                                        {
                                            await this.solutionService.Import((int)asyncJob.ActionNavigation.Solution, (int)asyncJob.ImportTargetEnvironment, asyncJob.ActionNavigation.CreatedByNavigation.MsId, isPatch);
                                        }
                                        catch (Exception e)
                                        {
                                            dbContext.Actions.Add(new Data.Models.Action
                                            {
                                                Name = $"{asyncJob.ActionNavigation.SolutionNavigation.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                                                TargetEnvironment = (int)asyncJob.ImportTargetEnvironment,
                                                Type = 2,
                                                Status = 3,
                                                Result = 2,
                                                ErrorMessage = e.Message,
                                                StartTime = DateTime.Now,
                                                Solution = asyncJob.ActionNavigation.Solution,
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    // Upgrade Apply Manually
                                    if (!isPatch)
                                    {
                                        Upgrade upgrade = (Upgrade)asyncJob.ActionNavigation.SolutionNavigation;
                                        if (upgrade.ApplyManually == false && asyncJob.IsManaged == true && asyncJob.ActionNavigation.Status != 4)
                                        {
                                            await DeleteAndPromote(asyncJob, token);

                                            AsyncJob newAsyncJob = await CreateAsyncJobForApplyingUpgrade(asyncJob, token);
                                            dbContext.Add(newAsyncJob);
                                        }
                                        else
                                        {
                                            UpdateSuccessfulAction(asyncJob);
                                        }
                                    }
                                    else
                                    {
                                        UpdateSuccessfulAction(asyncJob);
                                    }

                                }

                                dbContext.AsyncJobs.Remove(asyncJob);
                            }

                            // Completed Failed
                            else if ((string)responseData["statecode"] == "3" && (string)responseData["statuscode"] == "31")
                            {
                                string friendlyMessage = (string)responseData["friendlymessage"];
                                string exceptionMessage = null;
                                if (friendlyMessage == String.Empty)
                                {
                                    exceptionMessage = await GetExceptionMessage(asyncJob, token);
                                }
                                UpdateFailedAction(asyncJob, friendlyMessage == String.Empty ? exceptionMessage : friendlyMessage);
                                dbContext.AsyncJobs.Remove(asyncJob);
                            }

                        }
                    }
                }
                await dbContext.SaveChangesAsync();
            }
        }



        protected async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = DateTimeOffset.Now.AddSeconds(int.Parse(configuration["AsyncJobIntervalSeconds"]));
            var delay = next - DateTimeOffset.Now;
            if (delay.TotalMilliseconds <= 0) // prevent non-positive values from being passed into Timer
            {
                await ScheduleJob(cancellationToken);
            }
            timer = new System.Timers.Timer(delay.TotalMilliseconds);
            timer.Elapsed += async (sender, args) =>
            {
                timer.Dispose(); // reset and dispose timer
                timer = null;

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await DoWork(cancellationToken);
                    }
                    catch (System.Exception)
                    {

                        // TODO Logging;
                    }

                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await ScheduleJob(cancellationToken); // reschedule next
                }
            };
            timer.Start();
            await Task.CompletedTask;
        }

        private async Task<AsyncJob> CreateAsyncJobForApplyingUpgrade(AsyncJob asyncJob, string token)
        {
            Guid activityId = await GetActivitiyId(asyncJob, token);

            AsyncJob newAsyncJob = new AsyncJob
            {
                JobId = activityId,
                Action = asyncJob.Action,
                IsManaged = asyncJob.IsManaged
            };

            return newAsyncJob;
        }

        private async Task DeleteAndPromote(AsyncJob asyncJob, string token)
        {
            asyncJob.ActionNavigation.Status = 4;
            string apiUri = asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + "/DeleteAndPromote";
            JObject payload = new JObject();
            payload.Add("UniqueName", asyncJob.ActionNavigation.SolutionNavigation.UniqueName);
            StringContent payloadJson = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, mediaType: "application/json");
            await HttpPostRequest(apiUri, token, payloadJson);
        }
        private async Task<Guid> GetActivitiyId(AsyncJob asyncJob, string token)
        {
            string apiUri = asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + $"msdyn_solutionhistories?$orderby=msdyn_starttime%20desc&$filter=msdyn_name%20eq%20%27{asyncJob.ActionNavigation.SolutionNavigation.UniqueName}%27&$top=1";
            var responseMessage = await HttpGetRequest(apiUri, token);
            JObject responseMessageData = await responseMessage.Content.ReadAsAsync<JObject>();
            return (Guid)responseMessageData["value"][0]["msdyn_activityid"];
        }

        private async Task<JObject> GetSolutionHistory(AsyncJob asyncJob, string token)
        {
            string apiUri = asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + $"msdyn_solutionhistories?$filter=msdyn_activityid%20eq%20%27{asyncJob.JobId}%27";
            var responseMessage = await HttpGetRequest(apiUri, token);
            JObject responseMessageData = await responseMessage.Content.ReadAsAsync<JObject>();
            return responseMessageData;
        }

        private async Task<string> GetExceptionMessage(AsyncJob asyncJob, string token)
        {
            string apiUri = asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl + configuration["DownstreamApis:DataverseApi:BaseUrl"] + $"msdyn_solutionhistories?$orderby=msdyn_starttime%20desc&$filter=msdyn_name%20eq%20%27{asyncJob.ActionNavigation.SolutionNavigation.UniqueName}%27&$top=1";
            var responseMessage = await HttpGetRequest(apiUri, token);
            JObject responseMessageData = await responseMessage.Content.ReadAsAsync<JObject>();
            return (string)responseMessageData["value"][0]["msdyn_exceptionmessage"];
        }
    }
}