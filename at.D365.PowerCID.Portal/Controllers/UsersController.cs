using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Identity.Web;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.OData.Formatter;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace at.D365.PowerCID.Portal.Controllers
{
    public class UsersController : BaseController
    {

        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public UsersController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<UsersController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.configuration = configuration;
            this.logger = logger;

        }

        // GET: odata/Users
        [AllowAnonymous]
        [EnableQuery]
        public IQueryable<User> Get()
        {
            logger.LogDebug($"Begin & End: UsersController Get()");

            return base.dbContext.Users.Where(e => e.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromServices] IConfiguration configuration)
        {
            logger.LogDebug($"Begin: UsersController Login()");

            if (!ModelState.IsValid)
                return BadRequest();

            //get information of current logged in user
            User currentUser = new User
            {
                MsId = Guid.Parse(this.HttpContext.User.FindFirst(ClaimConstants.ObjectId)?.Value),
                Firstname = this.HttpContext.User.FindFirst(ClaimTypes.GivenName)?.Value,
                Lastname = this.HttpContext.User.FindFirst(ClaimTypes.Surname)?.Value,
                Email = this.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value
            };

            if (string.IsNullOrEmpty(currentUser.Firstname))
                currentUser.Firstname = currentUser.Email;

            Guid msIdTenantCurrentUser = Guid.Parse(this.HttpContext.User.FindFirst(ClaimConstants.TenantId).Value);
            bool newTenantCreated = false;

            //check if current user exists in database
            if (this.dbContext.Users.Any(e => e.MsId == currentUser.MsId))
            { //current user does exist in database
                currentUser = this.UpdateUserIfNeeded(currentUser);
            }
            else
            { //current user does NOT exist in database
                this.dbContext.Users.Add(currentUser);

                if (this.dbContext.Tenants.Any(e => e.MsId == msIdTenantCurrentUser))
                    currentUser.Tenant = this.dbContext.Tenants.First(e => e.MsId == msIdTenantCurrentUser).Id;
                else
                {
                    currentUser.TenantNavigation = await this.AddTenant(msIdTenantCurrentUser);
                    newTenantCreated = true;
                }
            }

            await this.dbContext.SaveChangesAsync();

            if (newTenantCreated || currentUser.MakeAdmin)
            {
                try
                {
                    Guid appId = Guid.Parse(configuration["AzureAd:ClientId"]);
                    Guid appRoleIdInitAdmin = Guid.Parse(configuration["AppRoleIdAdmin"]);
                    await this.AssignInitAdmin(appId, appRoleIdInitAdmin);
                    currentUser.MakeAdmin = false;
                }
                catch (Exception e)
                {
                    currentUser.MakeAdmin = true;
                }
                await this.dbContext.SaveChangesAsync();
            }
            logger.LogDebug($"End: UsersController Login()");

            return Ok();
        }


        private User UpdateUserIfNeeded(User currentUser)
        {
            logger.LogDebug($"Begin: UsersController UpdateUserIfNeeded(currentUser MsId: {currentUser.MsId})");

            User currentDbUser = this.dbContext.Users.First(e => e.MsId == currentUser.MsId);

            if (currentDbUser.Firstname != currentUser.Firstname)
                currentDbUser.Firstname = currentUser.Firstname;

            if (currentDbUser.Lastname != currentUser.Lastname)
                currentDbUser.Lastname = currentUser.Lastname;

            if (currentDbUser.Email != currentUser.Email)
                currentDbUser.Email = currentUser.Email;

            logger.LogDebug($"End: UsersController UpdateUserIfNeeded(currentUser MsId: {currentUser.MsId})");

            return currentDbUser;
        }

        private async Task<Tenant> AddTenant(Guid msId)
        {
            logger.LogDebug($"Begin: UsersController AddTenant(msId: {msId.ToString()})");

            string tenantName = await this.GetTenantName(msId);

            Tenant newTenant = new Tenant
            {
                Name = tenantName,
                MsId = msId
            };

            this.dbContext.Tenants.Add(newTenant);

            logger.LogDebug($"End: UsersController AddTenant(msId: {msId.ToString()})");

            return newTenant;
        }

        private async Task<HttpResponseMessage> CallWebApiForUserAsyncWithDataVerseApi(Data.Models.Environment environment, User user, HttpMethod httpMethod, string relativePath, StringContent content = null)
        {
            logger.LogDebug($"Begin: UsersController UpdateUserIfNeeded(environment BasicUrl: {environment.BasicUrl}, user TenantNavigation MsId: {user.TenantNavigation.MsId.ToString()}, relativePath: {relativePath})");

            if (content == null)
            {
                logger.LogInformation("content null");
                logger.LogDebug($"End: UsersController UpdateUserIfNeeded(environment BasicUrl: {environment.BasicUrl}, user TenantNavigation MsId: {user.TenantNavigation.MsId.ToString()}, relativePath: {relativePath})");

                return await this.downstreamWebApi.CallWebApiForUserAsync(
                               "DataverseApi",
                               options =>
                               {
                                   options.BaseUrl = environment.BasicUrl + options.BaseUrl;
                                   options.Tenant = $"{user.TenantNavigation.MsId}";
                                   options.RelativePath = relativePath;
                                   options.HttpMethod = httpMethod;
                                   options.Scopes = $"{environment.BasicUrl}/user_impersonation";
                               });
            }
            else
            {
                logger.LogInformation("content not null");
                logger.LogDebug($"End: UsersController UpdateUserIfNeeded(environment BasicUrl: {environment.BasicUrl}, user TenantNavigation MsId: {user.TenantNavigation.MsId.ToString()}, relativePath: {relativePath})");

                return await this.downstreamWebApi.CallWebApiForUserAsync(
                               "DataverseApi",
                               options =>
                               {
                                   options.BaseUrl = environment.BasicUrl + options.BaseUrl;
                                   options.Tenant = $"{user.TenantNavigation.MsId}";
                                   options.RelativePath = relativePath;
                                   options.HttpMethod = httpMethod;
                                   options.Scopes = $"{environment.BasicUrl}/user_impersonation";
                               }, content: content);
            }
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpPost]
        public async Task<IActionResult> SetupApplicationUsers()
        {
            logger.LogDebug($"Begin: UsersController SetupApplicationUsers()");

            Guid applicationId = configuration.GetValue<Guid>("AzureAd:ClientId");
            User currentUser = dbContext.Users.Include(x => x.TenantNavigation).FirstOrDefault(x => x.MsId == this.msIdCurrentUser);
            var environments = dbContext.Environments.Where(x => x.Tenant == currentUser.Tenant);
            List<string> environmentMessage = new List<string>();

            foreach (Data.Models.Environment environment in environments)
            {
                try
                {
                    //  GET systemcustomizerid
                    var responseSystemCustomizerRole = await CallWebApiForUserAsyncWithDataVerseApi(environment, currentUser, HttpMethod.Get, "/roles?$filter=_roletemplateid_value%20eq%20119f245c-3cc8-4b62-b31c-d1a046ced15d%20and%20ismanaged%20eq%20true");
                    JToken systemCostumizerRole = (await responseSystemCustomizerRole.Content.ReadAsAsync<JObject>())["value"];
                    if (systemCostumizerRole == null)
                    {
                        environmentMessage.Add($"{environment.Name}: error occurred while connection to environment");
                        continue;
                    }
                    Guid systemCustomizerId = (Guid)systemCostumizerRole[0]["roleid"];

                    // GET systemuser with that applicationid
                    var responseSystemUsers = await CallWebApiForUserAsyncWithDataVerseApi(environment, currentUser, HttpMethod.Get, $"/systemusers?$filter=applicationid%20eq%20{applicationId}");
                    JToken systemUsers = (await responseSystemUsers.Content.ReadAsAsync<JObject>())["value"];

                    // If systemuser doesnt exist in that environment
                    if (systemUsers.Count() == 0)
                    {
                        // GET businessunitid
                        var responseBusinessUnits = await CallWebApiForUserAsyncWithDataVerseApi(environment, currentUser, HttpMethod.Get, "/businessunits?$filter=_parentbusinessunitid_value%20eq%20null");
                        JToken businessUnits = (await responseBusinessUnits.Content.ReadAsAsync<JObject>())["value"];
                        Guid businessUnitId = (Guid)businessUnits[0]["businessunitid"];

                        // Create new systemuser
                        JObject newSystemUser = new JObject();
                        newSystemUser.Add("applicationid", applicationId);
                        newSystemUser.Add("businessunitid@odata.bind", $"/businessunits({businessUnitId})");
                        StringContent systemUserContent = new StringContent(JsonConvert.SerializeObject(newSystemUser), Encoding.UTF8, mediaType: "application/json");

                        // Post new systemuser
                        var responsePostSystemUser = await CallWebApiForUserAsyncWithDataVerseApi(environment, currentUser, HttpMethod.Post, $"/systemusers", systemUserContent);
                        //JToken postSystemUserContent = (await responsePostSystemUser.Content.ReadAsAsync<JObject>());

                        environmentMessage.Add($"{environment.Name}: new application user was created");

                        // Load new systemuser
                        responseSystemUsers = await CallWebApiForUserAsyncWithDataVerseApi(environment, currentUser, HttpMethod.Get, $"/systemusers?$filter=applicationid%20eq%20{applicationId}");
                        systemUsers = (await responseSystemUsers.Content.ReadAsAsync<JObject>())["value"];
                    }

                    Guid systemUserId = (Guid)systemUsers[0]["systemuserid"];

                    // GET systemuserrolescollection
                    var responseSystemUserRolesCollection = await CallWebApiForUserAsyncWithDataVerseApi(environment, currentUser, HttpMethod.Get, $"/systemuserrolescollection?$filter=roleid%20eq%20{systemCustomizerId}%20and%20systemuserid%20eq%20{systemUserId}");
                    JToken systemUserRolesCollection = (await responseSystemUserRolesCollection.Content.ReadAsAsync<JObject>())["value"];

                    if (systemUserRolesCollection.Count() == 0)
                    {
                        // Assign systemcustomizer role to user
                        JObject newSystemUserRole = new JObject();
                        newSystemUserRole.Add("@odata.id", $"{environment.BasicUrl}{configuration.GetValue<string>("DownstreamApis:DataverseApi:BaseUrl").TrimStart('/')}roles({systemCustomizerId})");
                        StringContent systemUserContent = new StringContent(JsonConvert.SerializeObject(newSystemUserRole), Encoding.UTF8, mediaType: "application/json");

                        var responsePostSystemUserRolesCollection = await CallWebApiForUserAsyncWithDataVerseApi(environment, currentUser, HttpMethod.Post, $"/systemusers({systemUserId})/systemuserroles_association/$ref?", systemUserContent);
                        if (responsePostSystemUserRolesCollection.IsSuccessStatusCode)
                            environmentMessage.Add($"{environment.Name}: System Customizer role was assigned to application user");
                        else
                        {
                            //JToken postSystemUserRolesCollection = (await responsePostSystemUserRolesCollection.Content.ReadAsAsync<JObject>())["value"];
                            //environmentMessage.Add($"{environment.Name}: {(string)postSystemUserRolesCollection[0]["error"]["message"]}");
                            environmentMessage.Add($"{environment.Name}: role assignment of the application user failed");
                        }
                    }
                    else
                    {
                        environmentMessage.Add($"{environment.Name}: application user with System Customizer role already exists");
                    }
                }

                catch (Exception ex)
                {
                    environmentMessage.Add($"{environment.Name}: an error occurred ({ex.Message})");
                }
            }
            logger.LogDebug($"End: UsersController SetupApplicationUsers()");

            return Ok(environmentMessage);
        }


        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpPost]
        public async Task<IEnumerable<UserRole>> GetUserRoles([FromODataUri] int key)
        {
            logger.LogDebug($"Begin: UsersController GetUserRoles(key: {key})");

            var user = base.dbContext.Users.FirstOrDefault(u => u.Id == key);

            logger.LogDebug($"End: UsersController GetUserRoles(key: {key})");

            return await this.GetUserRolesInAzure(user.MsId);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpPost]
        public async Task<IEnumerable<AppRole>> GetAppRoles([FromODataUri] int key, [FromServices] IConfiguration configuration)
        {
            logger.LogDebug($"Begin: UsersController GetUserRoles(key: {key})");

            var user = base.dbContext.Users.FirstOrDefault(u => u.Id == key);
            Guid appId = Guid.Parse(configuration["AzureAd:ClientId"]);
            Guid enterpriseAppId = await this.GetEnterpriseAppId(appId);

            var response = await this.downstreamWebApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = $"{user.TenantNavigation.MsId}";
                    options.RelativePath = $"/servicePrincipals/{enterpriseAppId}";
                    options.HttpMethod = HttpMethod.Get;
                });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not get app roles");
            }

            JToken application = (await response.Content.ReadAsAsync<JObject>());
            JToken appRolesAsJToken = application["appRoles"];

            List<AppRole> appRoles = new List<AppRole>();


            for (int i = 0; i < appRolesAsJToken.Count(); i++)
            {
                try
                {
                    string id = (string)appRolesAsJToken[i]["id"];
                    string displayName = (string)appRolesAsJToken[i]["displayName"];

                    appRoles.Add(new AppRole { Id = id, DisplayName = displayName });
                }
                catch { }
            }
            logger.LogDebug($"End: UsersController GetUserRoles(key: {key})");

            return appRoles;
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpPost]
        public async Task<IActionResult> AssignRole([FromODataUri] int key, ODataActionParameters parameters, [FromServices] IConfiguration configuration)
        {
            logger.LogDebug($"Begin: UsersController AssignRole(key: {key})");

            Guid principalId = Guid.Parse(parameters["principalId"].ToString());
            Guid appId = Guid.Parse(configuration["AzureAd:ClientId"]);
            Guid enterpriseAppId = await this.GetEnterpriseAppId(appId);
            Guid appRoleId = Guid.Parse(parameters["appRoleId"].ToString());

            var user = base.dbContext.Users.FirstOrDefault(u => u.Id == key);

            await this.AssignRoleInAzure(user.MsId, principalId, enterpriseAppId, appRoleId);

            logger.LogDebug($"End: UsersController AssignRole(key: {key})");

            return Ok();
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpPost]
        public async Task<IActionResult> RemoveAssignedRole([FromODataUri] int key, ODataActionParameters parameters)
        {
            logger.LogDebug($"Begin: UsersController RemoveAssignedRole(key: {key}, parameters roleAssignmentId: {parameters["roleAssignmentId"].ToString()} )");

            var user = base.dbContext.Users.FirstOrDefault(u => u.Id == key);

            string roleAssignmentId = parameters["roleAssignmentId"].ToString();
            var response = await this.downstreamWebApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = $"{user.TenantNavigation.MsId}";
                    options.RelativePath = $"/users/{user.MsId}/appRoleAssignments/{roleAssignmentId}";
                    options.HttpMethod = HttpMethod.Delete;
                });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not remove role");
            }
            logger.LogDebug($"End: UsersController RemoveAssignedRole(key: {key}, parameters roleAssignmentId: {parameters["roleAssignmentId"].ToString()})");

            return Ok();
        }

        private async Task<string> GetTenantName(Guid msId)
        {
            logger.LogDebug($"Begin: UsersController GetTenantName(msId: {msId.ToString()})");

            var tenantResponse = await this.downstreamWebApi.CallWebApiForUserAsync(
                "AzureManagementApi",
                options =>
                {
                    options.RelativePath = "tenants?api-version=2020-01-01";
                });

            JToken tenants = (await tenantResponse.Content.ReadAsAsync<JObject>())["value"];

            for (int i = 0; i < tenants.Count(); i++)
            {
                if (Guid.Parse((string)tenants[i]["tenantId"]) == msId)
                    return (string)tenants[i]["displayName"];
            }

            logger.LogDebug($"End: UsersController GetTenantName(msId: {msId.ToString()})");

            //no tenant with msId was found
            throw new System.Exception($"can not find display name of tenant with id '{msId}'");
        }

        private async Task<Guid> GetEnterpriseAppId(Guid appId)
        {
            logger.LogDebug($"Begin: UsersController GetEnterpriseAppId(appId: {appId.ToString()})");

            var user = base.dbContext.Users.FirstOrDefault(u => u.MsId == this.msIdCurrentUser);

            var response = await this.downstreamWebApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = $"{user.TenantNavigation.MsId}";
                    options.RelativePath = $"/servicePrincipals?$filter=appId eq '{appId}'";
                    options.HttpMethod = HttpMethod.Get;
                });

            try
            {
                JToken enterpriseApp = (await response.Content.ReadAsAsync<JObject>())["value"][0];
                Guid enterpriseAppId = Guid.Parse((string)enterpriseApp["id"]);

                logger.LogDebug($"End: UsersController GetEnterpriseAppId(appId: {appId.ToString()})");

                return enterpriseAppId;
            }
            catch
            {
                throw new Exception("No Enterprise Application found.");
            }
        }

        private async Task AssignInitAdmin(Guid appId, Guid appRoleId)
        {
            logger.LogDebug($"Begin: UsersController AssignInitAdmin(appId: {appId.ToString()}, appRoleId: {appRoleId.ToString()})");

            List<UserRole> existingRoles = await this.GetUserRolesInAzure(this.msIdCurrentUser);

            if (!existingRoles.Any(e => e.AppRoleId == appRoleId.ToString()))
            {
                Guid enterpriseAppId = await this.GetEnterpriseAppId(appId);
                await this.AssignRoleInAzure(this.msIdCurrentUser, this.msIdCurrentUser, enterpriseAppId, appRoleId);
            }
            logger.LogDebug($"End: UsersController AssignInitAdmin(appId: {appId.ToString()}, appRoleId: {appRoleId.ToString()})");

        }

        private async Task<List<UserRole>> GetUserRolesInAzure(Guid msIdUser)
        {
            logger.LogDebug($"Begin: UsersController GetUserRolesInAzure(msIdUser: {msIdUser.ToString()})");

            var response = await this.downstreamWebApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = $"{this.msIdTenantCurrentUser}";
                    options.RelativePath = $"/users/{msIdUser}/appRoleAssignments";
                    options.HttpMethod = HttpMethod.Get;
                    options.TokenAcquisitionOptions = new TokenAcquisitionOptions { ForceRefresh = true };
                });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not get user roles");
            }

            JToken userRolesJToken = (await response.Content.ReadAsAsync<JObject>())["value"];

            List<UserRole> userRoles = new List<UserRole>();

            for (int i = 0; i < userRolesJToken.Count(); i++)
            {
                try
                {
                    string id = (string)userRolesJToken[i]["id"];
                    string resourceId = (string)userRolesJToken[i]["resourceId"];
                    string appRoleId = (string)userRolesJToken[i]["appRoleId"];

                    userRoles.Add(new UserRole { Id = id, ResourceId = resourceId, AppRoleId = appRoleId });
                }
                catch { }
            }
            logger.LogDebug($"End: UsersController GetUserRolesInAzure(msIdUser: {msIdUser.ToString()})");

            return userRoles;
        }

        private async Task AssignRoleInAzure(Guid msIdUser, Guid principalId, Guid resourceId, Guid appRoleId)
        {
            logger.LogDebug($"Begin: UsersController GetUserRolesInAzure(msIdUser: {msIdUser.ToString()}, principalId: {principalId.ToString()}, appRoleId: {appRoleId.ToString()})");

            JObject newRoleAssignment = new JObject();
            newRoleAssignment.Add("principalId", principalId);
            newRoleAssignment.Add("resourceId", resourceId);
            newRoleAssignment.Add("appRoleId", appRoleId);

            StringContent roleContent = new StringContent(JsonConvert.SerializeObject(newRoleAssignment), Encoding.UTF8, mediaType: "application/json");

            var response = await this.downstreamWebApi.CallWebApiForUserAsync(
                "GraphApi",
                options =>
                {
                    options.Tenant = $"{this.msIdTenantCurrentUser}";
                    options.RelativePath = $"/users/{msIdUser}/appRoleAssignments";
                    options.HttpMethod = HttpMethod.Post;
                }, content: roleContent);

            logger.LogDebug($"End: UsersController GetUserRolesInAzure(msIdUser: {msIdUser.ToString()}, principalId: {principalId.ToString()}, appRoleId: {appRoleId.ToString()})");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not assign role");
            }
        }
    }
}
