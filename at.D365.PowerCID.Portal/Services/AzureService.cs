using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace at.D365.PowerCID.Portal.Services
{
    public class AzureService
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        public AzureService(IServiceProvider serviceProvider, ILogger<AzureService> logger)
        {
            var scope = serviceProvider.CreateScope();
            this.configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            this.logger = logger;
        }

        public async Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsOfUser(IDownstreamWebApi webApi, Guid tenantMsId, Guid userMsId){
            logger.LogDebug($"Begin: AzureService GetAppRoleIdsOfUser(tenantMsId: {tenantMsId.ToString()}, userMsId: {userMsId.ToString()})");

            var response = await webApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = tenantMsId.ToString();
                    options.RelativePath = $"/users/{userMsId.ToString()}/appRoleAssignments";
                    options.HttpMethod = HttpMethod.Get;
                    options.TokenAcquisitionOptions = new TokenAcquisitionOptions { ForceRefresh = true };
                });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not get AppRoleAssignments");
            }

            JToken appRoleAssignments = (await response.Content.ReadAsAsync<JObject>())["value"];

            List<AppRoleAssignment> appRoleIds = new List<AppRoleAssignment>();

            foreach (var userRoleJToken in appRoleAssignments)
            {
                appRoleIds.Add(new AppRoleAssignment() {
                    Id = (string)userRoleJToken["id"],
                    AppRoleId = Guid.Parse((string)userRoleJToken["appRoleId"])
                });
            }

            logger.LogDebug($"End: AzureService GetAppRoleIdsOfUser(Count appRoleIds: {appRoleIds.Count})");
            
            return appRoleIds;
        }

        public async Task AssignAppRoleToUser(IDownstreamWebApi webApi, Guid tenantMsId, Guid userMsId, Guid appRoleId){
            logger.LogDebug($"Begin: AzureService AssignAppRole(tenantMsId: {tenantMsId.ToString()}, userMsId: {userMsId.ToString()}, appRoleId: {appRoleId.ToString()})");

            var enterpriseAppId = await this.GetEnterpriseAppId(webApi, tenantMsId);

            JObject newRoleAssignment = new JObject();
            newRoleAssignment.Add("principalId", userMsId);
            newRoleAssignment.Add("resourceId", enterpriseAppId);
            newRoleAssignment.Add("appRoleId", appRoleId);

            StringContent roleContent = new StringContent(JsonConvert.SerializeObject(newRoleAssignment), Encoding.UTF8, mediaType: "application/json");

            var response = await webApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = tenantMsId.ToString();
                    options.RelativePath = $"/users/{userMsId.ToString()}/appRoleAssignments";
                    options.HttpMethod = HttpMethod.Post;
                }, content: roleContent);

            if (!response.IsSuccessStatusCode)
                throw new Exception("Could not assign role");

            logger.LogDebug($"End: AzureService AssignAppRole()");
        }

        public async Task RemoveAppRoleFromUser(IDownstreamWebApi webApi, Guid tenantMsId, Guid userMsId, string appRoleAssignmentId){
            logger.LogDebug($"Begin: AzureService RemoveAppRole(tenantMsId: {tenantMsId.ToString()}, userMsId: {userMsId.ToString()}, userMsId: {appRoleAssignmentId.ToString()})");

            var response = await webApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = tenantMsId.ToString();
                    options.RelativePath = $"/users/{userMsId.ToString()}/appRoleAssignments/{appRoleAssignmentId}";
                    options.HttpMethod = HttpMethod.Delete;
                });

            if (!response.IsSuccessStatusCode)
                throw new Exception("Could not remove role");

            logger.LogDebug($"End: AzureService RemoveAssignedRole()");
        }

        public async Task<string> GetTenantName(IDownstreamWebApi webApi, Guid msId)
        {
            logger.LogDebug($"Begin: AzureService GetTenantName(msId: {msId.ToString()})");

            var tenantResponse = await webApi.CallWebApiForUserAsync(
                "AzureManagementApi",
                options =>
                {
                    options.RelativePath = "tenants?api-version=2020-01-01";
                });

            JToken tenants = (await tenantResponse.Content.ReadAsAsync<JObject>())["value"];

            string tenantName = "";

            foreach (var tenant in tenants)
            {
                if (Guid.Parse((string)tenant["tenantId"]) == msId){
                    tenantName = (string)tenant["displayName"];
                    break;
                }
            }

            if(String.IsNullOrEmpty(tenantName)) //no tenant with msId was found
                throw new System.Exception($"can not find display name of tenant with id '{msId}'");

            logger.LogDebug($"End: AzureService GetTenantName(tenantName: {tenantName})");
            return tenantName;
        }


        private async Task<Guid> GetEnterpriseAppId(IDownstreamWebApi webApi, Guid tenantMsId)
        {
            logger.LogDebug($"Begin: AzureService GetEnterpriseAppId(tenantMsId: {tenantMsId.ToString()})");

            Guid appId = Guid.Parse(configuration["AzureAd:ClientId"]);

            var response = await webApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = tenantMsId.ToString();
                    options.RelativePath = $"/servicePrincipals?$filter=appId eq '{appId}'";
                    options.HttpMethod = HttpMethod.Get;
                });

            try
            {
                JToken enterpriseApp = (await response.Content.ReadAsAsync<JObject>())["value"][0];
                Guid enterpriseAppId = Guid.Parse((string)enterpriseApp["id"]);

                logger.LogDebug($"End: AzureService GetEnterpriseAppId(enterpriseAppId: {enterpriseAppId.ToString()})");

                return enterpriseAppId;
            }
            catch{
                throw new Exception("No Enterprise Application found.");
            }
        }
    }
}