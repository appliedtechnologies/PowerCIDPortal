using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using Environment = at.D365.PowerCID.Portal.Data.Models.Environment;

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
                        await DoBackgroundWork(cancellationToken);
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

        private async Task DoBackgroundWork(CancellationToken cancellationToken)
        {

            var asyncJobsByEnvironment = dbContext.AsyncJobs.ToList().GroupBy(e => e.ActionNavigation.TargetEnvironment);

            if (asyncJobsByEnvironment.Count() > 0)
            {
                foreach (var environmentGroup in asyncJobsByEnvironment)
                {
                    try{
                        var environmentId = environmentGroup.Key;
                        Environment environment = dbContext.Environments.Find(environmentId);

                        foreach (AsyncJob asyncJob in environmentGroup)
                        {
                            try{
                                switch(asyncJob.ActionNavigation.Type){
                                    case 1: //export
                                    {
                                        Entity asyncOperationInDataverse = await this.GetCurrentAsyncOperationFromDataverse(environment.BasicUrl, (Guid)asyncJob.AsyncOperationId);

                                        if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 30){ // Completed Success
                                            string exportSolutionFile = await this.solutionService.DownloadSolutionFileFromDataverse(asyncJob);
                                            await this.gitHubService.SaveSolutionFile(asyncJob, exportSolutionFile, environment.TenantNavigation);
                                            UpdateSuccessfulAction(asyncJob);

                                            // Export with Import --> Start Import
                                            if (asyncJob.ActionNavigation.ExportOnly == false && asyncJob.IsManaged == true)
                                            {
                                                try
                                                {
                                                    await this.solutionService.AddImportAction((int)asyncJob.ActionNavigation.Solution, (int)asyncJob.ActionNavigation.ImportTargetEnvironment, asyncJob.ActionNavigation.CreatedByNavigation.MsId);
                                                }
                                                catch (Exception e)
                                                {
                                                    dbContext.Actions.Add(new Data.Models.Action
                                                    {
                                                        Name = $"{asyncJob.ActionNavigation.SolutionNavigation.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                                                        TargetEnvironment = (int)asyncJob.ActionNavigation.ImportTargetEnvironment,
                                                        Type = 2,
                                                        Status = 3,
                                                        Result = 2,
                                                        ErrorMessage = e.Message,
                                                        StartTime = DateTime.Now,
                                                        Solution = asyncJob.ActionNavigation.Solution,
                                                    });
                                                }
                                            }
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                        else if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 31) // Completed Failed
                                        {
                                            await UpdateFailedAction(asyncJob, (string)asyncOperationInDataverse["friendlymessage"]);
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                    }
                                    break;
                                    case 2: //import
                                    {
                                        Entity asyncOperationInDataverse = await this.GetCurrentAsyncOperationFromDataverse(environment.BasicUrl, (Guid)asyncJob.AsyncOperationId);

                                        if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 30){ // Completed Success
                                            UpdateSuccessfulAction(asyncJob);

                                            // Upgrade Solution without manuelly upgrade apply --> Start Apply Upgrade
                                            bool isPatch = asyncJob.ActionNavigation.SolutionNavigation.GetType().Name.Contains("Patch");
                                            if (!isPatch)
                                            {
                                                Upgrade upgrade = (Upgrade)asyncJob.ActionNavigation.SolutionNavigation;
                                                if (upgrade.ApplyManually == false && asyncJob.IsManaged == true && asyncJob.ActionNavigation.Status != 4)
                                                {
                                                    try
                                                    {
                                                        await this.solutionService.AddApplyUpgradeAction((int)asyncJob.ActionNavigation.Solution, (int)asyncJob.ActionNavigation.TargetEnvironment, asyncJob.ActionNavigation.CreatedByNavigation.MsId);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        dbContext.Actions.Add(new Data.Models.Action
                                                        {
                                                            Name = $"{asyncJob.ActionNavigation.SolutionNavigation.Name}_{DateTimeOffset.Now.ToUnixTimeSeconds()}",
                                                            TargetEnvironment = (int)asyncJob.ActionNavigation.ImportTargetEnvironment,
                                                            Type = 3,
                                                            Status = 3,
                                                            Result = 2,
                                                            ErrorMessage = e.Message,
                                                            StartTime = DateTime.Now,
                                                            Solution = asyncJob.ActionNavigation.Solution,
                                                        });
                                                    }
                                                }
                                            }
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                        else if (((OptionSetValue)asyncOperationInDataverse["statecode"]).Value == 3 && ((OptionSetValue)asyncOperationInDataverse["statuscode"]).Value == 31) // Completed Failed
                                        {
                                            await UpdateFailedAction(asyncJob, (string)asyncOperationInDataverse["friendlymessage"]);
                                            dbContext.AsyncJobs.Remove(asyncJob);
                                        }
                                    }
                                    break;
                                    case 3: //appling upgrade
                                        /*JObject responseMessageData = await GetSolutionHistory(asyncJob, token);
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
                                        }*/
                                        break;
                                    default:
                                        throw new Exception($"Unknow ActionType: {asyncJob.ActionNavigation.Type}");
                                }
                                await dbContext.SaveChangesAsync(asyncJob.ActionNavigation.CreatedByNavigation.MsId);                              
                            }
                            catch(Exception e){
                                //TODO logging
                                continue;
                            }
                        }
                    }
                    catch(Exception e){
                        //TODO logging
                        continue;
                    }
                }
            }
        }

        private void UpdateSuccessfulAction(AsyncJob asyncJob)
        {
            asyncJob.ActionNavigation.Status = 3;
            asyncJob.ActionNavigation.Result = 1;
            asyncJob.ActionNavigation.FinishTime = DateTime.Now;
        }

        private async Task UpdateFailedAction(AsyncJob asyncJob, string friendlyErrormessage)
        {
            asyncJob.ActionNavigation.Status = 3;
            asyncJob.ActionNavigation.Result = 2;
            asyncJob.ActionNavigation.FinishTime = DateTime.Now;

            asyncJob.ActionNavigation.ErrorMessage = friendlyErrormessage;
            if (asyncJob.ActionNavigation.ErrorMessage == String.Empty)
            {
                asyncJob.ActionNavigation.ErrorMessage = await GetExceptionMessage(asyncJob);
            }
        }

        private async Task<Entity> GetCurrentAsyncOperationFromDataverse(string basicUrl, Guid asyncOperationId){
            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                Entity response = await dataverseClient.RetrieveAsync("asyncoperation", asyncOperationId, new ColumnSet("asyncoperationid", "statecode", "statuscode", "friendlymessage"));
                return response;
            }
        }

        //down: TODO

        /*private async Task<AsyncJob> CreateAsyncJobForApplyingUpgrade(AsyncJob asyncJob, string token)
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
        }*/

        private async Task<string> GetExceptionMessage(AsyncJob asyncJob)
        {
            using(var dataverseClient = new ServiceClient(new Uri(asyncJob.ActionNavigation.TargetEnvironmentNavigation.BasicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], false)){
                var query = new QueryExpression("msdyn_solutionhistory"){
                    ColumnSet = new ColumnSet("msdyn_exceptionmessage"),
                    PageInfo = new PagingInfo(){
                        Count = 1,
                        PageNumber = 1 
                    }
                };
                query.Criteria.AddCondition("msdyn_name", ConditionOperator.Equal, new [] {asyncJob.ActionNavigation.SolutionNavigation.UniqueName});
                query.Criteria.AddCondition("msdyn_solutionversion", ConditionOperator.Equal, new [] {asyncJob.ActionNavigation.SolutionNavigation.Version});

                query.AddOrder("msdyn_starttime", OrderType.Descending);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);
                return (string)response.Entities.FirstOrDefault()?["msdyn_exceptionmessage"];
            }
        }
    }
}