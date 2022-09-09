using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class EnvironmentsController : BaseController
    {
        private readonly ILogger logger;

        public EnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<EnvironmentsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        // GET: odata/Environments
        [EnableQuery]
        public IQueryable<at.D365.PowerCID.Portal.Data.Models.Environment> Get()
        {
            logger.LogDebug("Begin & End: EnvironmentsController Get()");

            return base.dbContext.Environments.Where(e => e.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Environment> environment)
        {
            logger.LogDebug($"Begin: EnvironmentsController Patch(key: {key}, environment: {environment.GetChangedPropertyNames().ToString()}");

            if ((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == key && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            string[] propertyNamesAllowedToChange = { "OrdinalNumber", "IsDevelopmentEnvironment", "ConnectionsOwner", "DeployUnmanaged" };
            if (environment.GetChangedPropertyNames().Except(propertyNamesAllowedToChange).Count() == 0)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var entity = await base.dbContext.Environments.FindAsync(key);
                if (entity == null)
                {
                    return NotFound();
                }
                environment.Patch(entity);
                try
                {
                    await base.dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EnvironmentExists(key))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                logger.LogDebug($"End: EnvironmentsController Patch(key: {key}, environment: {environment.GetChangedPropertyNames().ToString()}");

                return Updated(entity);
            }
            else
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> PullExisting()
        {
            logger.LogDebug("Begin: EnvironmentsController PullExisting()");

            IEnumerable<Environment> pulledEnvironments = await this.GetExistingEnvironments();

            //update existing environments
            var pulledAlreadyExistingEnvironments = pulledEnvironments.Where(e => this.dbContext.Environments.Select(t => t.MsId).Contains(e.MsId));
            foreach (Environment pulledEnvironment in pulledAlreadyExistingEnvironments)
                this.UpdateEnvironmentIfNeeded(pulledEnvironment);

            //add NOT existing environments
            var pulledNotExisting = pulledEnvironments.Except(pulledAlreadyExistingEnvironments);
            await this.dbContext.Environments.AddRangeAsync(pulledNotExisting);

            await this.dbContext.SaveChangesAsync();

            logger.LogDebug("End: EnvironmentsController PullExisting()");

            return Ok();
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> GetDataversePublishers([FromODataUri] int key)
        {
            logger.LogDebug($"Begin: EnvironmentsController GetDataversePublishers(key: {key})");

            Environment environment = await this.dbContext.Environments.FindAsync(key);

            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{this.msIdTenantCurrentUser}";
                options.BaseUrl = environment.BasicUrl + options.BaseUrl;
                options.RelativePath = "/publishers?$orderby=friendlyname";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{environment.BasicUrl}/.default";
            });

            if (!response.IsSuccessStatusCode)
            {
                throw new System.Exception("Could not get Publishers");
            }

            JObject reponseData = await response.Content.ReadAsAsync<JObject>();
            var publishersFromApi = reponseData["value"].ToList();
            var publishers = new List<dynamic>();

            foreach (var publisher in publishersFromApi)
            {
                publishers.Add(new
                {
                    friendlyname = (string)publisher["friendlyname"],
                    publisherid = (string)publisher["publisherid"],
                    isreadonly = (bool)publisher["isreadonly"]
                });
            }
            logger.LogDebug($"End: EnvironmentsController GetDataversePublishers(key: {key})");

            return Ok(publishers);
        }

        private void UpdateEnvironmentIfNeeded(Environment pulledEnvironment)
        {
            logger.LogDebug($"Begin: EnvironmentsController UpdateEnvironmentIfNeeded(pulledEnvironment Name: {pulledEnvironment.Name})");

            Environment currentDbEnvironment = this.dbContext.Environments.First(e => e.MsId == pulledEnvironment.MsId);

            if (currentDbEnvironment.Name != pulledEnvironment.Name)
                currentDbEnvironment.Name = pulledEnvironment.Name;

            if (currentDbEnvironment.BasicUrl != pulledEnvironment.BasicUrl)
                currentDbEnvironment.BasicUrl = pulledEnvironment.BasicUrl;

            logger.LogDebug($"End: EnvironmentsController UpdateEnvironmentIfNeeded(pulledEnvironment Name: {pulledEnvironment.Name})");
        }

        private async Task<IEnumerable<Environment>> GetExistingEnvironments()
        {
            logger.LogDebug("Begin: EnvironmentsController GetExistingEnvironments()");

            var environmentsRepsonse = await this.downstreamWebApi.CallWebApiForUserAsync(
                "AzureManagementApi",
                options =>
                {
                    options.RelativePath = "providers/Microsoft.ProcessSimple/environments?api-version=2016-11-01";
                });

            JToken remoteEnvironments = (await environmentsRepsonse.Content.ReadAsAsync<JObject>())["value"];
            List<Environment> environments = new List<Environment>();
            Tenant currentUsersTenant = this.dbContext.Tenants.First(e => e.MsId == this.msIdTenantCurrentUser);

            for (int i = 0; i < remoteEnvironments.Count(); i++)
            {
                try
                {
                    string dirtyMsId = (string)remoteEnvironments[i]["name"];
                    if (dirtyMsId.Count(e => e == '-') == 5)
                        dirtyMsId = dirtyMsId.Substring(dirtyMsId.IndexOf('-') + 1);

                    Environment environment = new Environment
                    {
                        Name = (string)remoteEnvironments[i]["properties"]["linkedEnvironmentMetadata"]["friendlyName"],
                        BasicUrl = (string)remoteEnvironments[i]["properties"]["linkedEnvironmentMetadata"]["instanceUrl"],
                        MsId = System.Guid.Parse(dirtyMsId),
                        Tenant = currentUsersTenant.Id
                    };
                    environments.Add(environment);
                }
                catch { }
            }
            logger.LogDebug("End: EnvironmentsController GetExistingEnvironments()");

            return environments;
        }

        private bool EnvironmentExists(int key)
        {
            logger.LogDebug($"Begin & End: EnvironmentsController EnvironmentExists(key: {key})");

            return base.dbContext.Environments.Any(p => p.Id == key);
        }
    }
}
