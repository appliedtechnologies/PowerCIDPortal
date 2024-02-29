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
        private readonly atPowerCIDContext dbContext;

        public AzureService(atPowerCIDContext dbContext, ILogger<AzureService> logger, IConfiguration configuration)
        {
            this.dbContext  = dbContext;
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task AdminRoleSync(IDownstreamWebApi webApi, Guid tenantMsId){
            logger.LogDebug($"Begin: AzureService AdminRoleSync(tenantMsId: {tenantMsId})");

            var appRoleIdAdmin = Guid.Parse(configuration["AppRoleIds:Admin"]);
            var ownerMsIds = await this.GetApplicationOwnerMsIds(webApi, tenantMsId);

            foreach(var user in this.dbContext.Users.Where(e => e.TenantNavigation.MsId == tenantMsId && (e.IsOwner || e.RemoveAdminRole))){
                if(!ownerMsIds.Contains(user.MsId)){
                    var existingRoles = await this.GetAppRoleAssignmentsOfUser(webApi, tenantMsId, user.MsId);

                    if (existingRoles.Any(e => e.AppRoleId == appRoleIdAdmin)){
                        await this.RemoveAppRoleFromUser(webApi, tenantMsId, user.MsId, existingRoles.First(e => e.AppRoleId == appRoleIdAdmin).Id);
                    }
                    else
                        logger.LogInformation($"User with MsId {user.MsId} has Admin no role)");

                    user.IsOwner = false;
                    user.RemoveAdminRole = false;
                }
            }

            foreach(var ownerMsId in ownerMsIds){
                var user = this.dbContext.Users.FirstOrDefault(e => e.TenantNavigation.MsId == tenantMsId && e.MsId == ownerMsId);
                if(user != null && !user.IsOwner){
                    if(!(await this.GetAppRoleAssignmentsOfUser(webApi, tenantMsId, ownerMsId)).Any(e => e.AppRoleId == appRoleIdAdmin))
                        await this.AssignAppRoleToUser(webApi, tenantMsId, ownerMsId, appRoleIdAdmin);
                    user.IsOwner = true;
                }
            }

            logger.LogDebug($"End: AzureService AdminRoleSync(tenantMsId: {tenantMsId})");
        }

        public async Task<IEnumerable<Guid>> GetApplicationOwnerMsIds(IDownstreamWebApi webApi, Guid tenantMsId){
            logger.LogDebug($"Begin: AzureService GetApplicationOwnerMsIds(tenantMsId: {tenantMsId})");

            var enterpriseAppId = await this.GetEnterpriseAppId(webApi, tenantMsId);

            var response = await webApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = tenantMsId.ToString();
                    options.RelativePath = $"/servicePrincipals/{enterpriseAppId}/owners";
                    options.HttpMethod = HttpMethod.Get;
                });

            if (!response.IsSuccessStatusCode)
                throw new Exception("Could not get application owner");

            JToken owners = (await response.Content.ReadAsAsync<JObject>())["value"];

            var ownerMsIds = new List<Guid>();

            foreach (var owner in owners)
                ownerMsIds.Add(Guid.Parse((string)owner["id"]));

            logger.LogDebug($"End: AzureService GetApplicationOwnerMsIds(Count ownerMsIds: {ownerMsIds.Count})");
            
            return ownerMsIds;
        }

        public async Task<bool> IsApplicationOwner(IDownstreamWebApi webApi, Guid tenantMsId, Guid userMsId){
            logger.LogDebug($"Begin: AzureService IsApplicationOwner(tenantMsId: {tenantMsId}, userMsId: {userMsId})");

            var ownerMsIds = await this.GetApplicationOwnerMsIds(webApi, tenantMsId);
            bool isUserOwner = ownerMsIds.Any(e => e == userMsId);

            logger.LogDebug($"End: AzureService IsApplicationOwner(isUserOwner: {isUserOwner})");
            
            return isUserOwner;
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

            logger.LogDebug($"Graph Request Body: {tenantMsId}");

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
            JToken enterpriseApp = (await response.Content.ReadAsAsync<JObject>())["value"][0];
            Guid enterpriseAppId = Guid.Parse((string)enterpriseApp["id"]);

            logger.LogDebug($"End: AzureService GetEnterpriseAppId(enterpriseAppId: {enterpriseAppId.ToString()})");

            return enterpriseAppId;
        }
    }
}