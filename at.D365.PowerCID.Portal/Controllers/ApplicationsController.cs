using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using at.D365.PowerCID.Portal.Services;

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class ApplicationsController : BaseController
    {
        public ApplicationsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        [EnableQuery]
        public IQueryable<Application> Get([FromODataUri] int key)
        {
            return base.dbContext.Applications.Where(e => e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser && e.Id == key);
        }

        // GET: odata/Applications
        [EnableQuery]
        public IQueryable<Application> Get()
        {
            return base.dbContext.Applications.Where(e => e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }


        // POST
        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        public async Task<IActionResult> Post([FromBody] Application application, [FromServices] SolutionService solutionService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == application.DevelopmentEnvironment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            if (base.dbContext.Applications.Any(a => (a.DevelopmentEnvironment == application.DevelopmentEnvironment) && (a.Name == application.Name || a.SolutionUniqueName == application.SolutionUniqueName)))
            {
                return BadRequest("Application already exists with that name");
            }

            if(application.OrdinalNumber == null){
                var existingApplications = this.dbContext.Applications.Where(e => e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
                var existingMaxOrdinalNumber = existingApplications.Max(e => e.OrdinalNumber);
                application.OrdinalNumber = existingMaxOrdinalNumber != null ? existingMaxOrdinalNumber + 1 : 1;
            }

            await CreateSolutionInDataverse(application);
            var publisher = base.dbContext.Publishers.FirstOrDefault(p => p.MsId == application.PublisherNavigation.MsId);

            if (publisher != null)
            {
                if (publisher.Name != application.PublisherNavigation.Name)
                {
                    publisher.Name = application.PublisherNavigation.Name;
                }
                application.Publisher = publisher.Id;
                application.PublisherNavigation = null;
            }
            else{
                application.PublisherNavigation.Environment = application.DevelopmentEnvironment;
            }

            base.dbContext.Applications.Add(application);
            await base.dbContext.SaveChangesAsync();

            await CreateNewUpgrade(application, version: null, solutionService);

            return Created(application);
        }


        //PATCH
        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Application> application)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if((await this.dbContext.Applications.FirstOrDefaultAsync(e => e.Id == key && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            string[] propertyNamesAllowedToChange = { nameof(Application.OrdinalNumber), nameof(Application.Name), nameof(Application.MsId), nameof(Application.InternalDescription) };
            if (application.GetChangedPropertyNames().Except(propertyNamesAllowedToChange).Count() != 0)
            {
                return BadRequest();
            }
            var entity = await base.dbContext.Applications.FindAsync(key);
            if (entity == null)
            {
                return NotFound();
            }
            application.Patch(entity);
            try
            {
                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApplicationExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(entity);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            var application = await dbContext.Applications.FindAsync(key);

            if((await this.dbContext.Applications.FirstOrDefaultAsync(e => e.Id == key && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            if (application == null)
            {
                return NotFound();
            }
            dbContext.Applications.Remove(application);
            await dbContext.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public string GetMakerPortalUrl([FromODataUri] int key)
        {
            var lastSolution = this.dbContext.Solutions.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser && e.Application == key).OrderBy(e => e.CreatedOn).Last();
            return lastSolution.UrlMakerportal;
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> PullExisting([FromBody] ODataActionParameters parameters)
        {
            List<string> applicationUniqueNames = new List<string>();
            User user = base.dbContext.Users.FirstOrDefault(u => u.MsId == dbContext.MsIdCurrentUser);
            at.D365.PowerCID.Portal.Data.Models.Environment environment = (at.D365.PowerCID.Portal.Data.Models.Environment)parameters["environment"];

            if((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == environment.Id && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            // Request solutions that are not patches
            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{user.TenantNavigation.MsId}";
                options.BaseUrl = environment.BasicUrl + options.BaseUrl;
                options.RelativePath = "/solutions?$filter=ismanaged%20eq%20false%20and%20_parentsolutionid_value%20eq%20null&$select=uniquename&$orderby=uniquename%20asc";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{environment.BasicUrl}/.default";
            });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not get Solutions");
                //Hallo
            }

            // Response
            JObject reponseData = await response.Content.ReadAsAsync<JObject>();
            JToken solutions = reponseData["value"];

            // Add uniquenames to list
            for (int i = 0; i < solutions.Count(); i++)
            {
                applicationUniqueNames.Add((string)solutions[i]["uniquename"]);
            }

            return Ok(applicationUniqueNames);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> SaveApplication([FromBody] ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            Application application;
            string applicationUniqueName = (string)parameters["applicationUniqueName"];
            at.D365.PowerCID.Portal.Data.Models.Environment environment = (at.D365.PowerCID.Portal.Data.Models.Environment)parameters["environment"];
            User user = base.dbContext.Users.FirstOrDefault(u => u.MsId == dbContext.MsIdCurrentUser);

            if((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == environment.Id && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            // Check if application already exists
            if (base.dbContext.Applications.Any(a => a.SolutionUniqueName == applicationUniqueName))
            {
                return BadRequest("Application already exists with that name");
            }

            // Request solution where uniquename = selected uniquename
            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{user.TenantNavigation.MsId}";
                options.BaseUrl = environment.BasicUrl + options.BaseUrl;
                options.RelativePath = $"/solutions?$filter=uniquename%20eq%20%27{applicationUniqueName}%27";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{environment.BasicUrl}/.default";
            });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not get solution with that uniquename");
            }

            // Response
            JObject reponseData = await response.Content.ReadAsAsync<JObject>();
            JToken solution = reponseData["value"];

            // Publisher in database?
            if (dbContext.Publishers.Any(p => p.MsId == (Guid)solution[0]["_publisherid_value"]) == false)
            {
                Publisher newPublisher = await CreateNewPublisher((Guid)solution[0]["_publisherid_value"], user, environment);
                dbContext.Add(newPublisher);
                await dbContext.SaveChangesAsync();
            }
            int publisher = dbContext.Publishers.FirstOrDefault(p => p.MsId == (Guid)solution[0]["_publisherid_value"]).Id;

            // New application
            application = new Application
            {
                Name = (string)solution[0]["friendlyname"],
                SolutionUniqueName = applicationUniqueName,
                DevelopmentEnvironment = environment.Id,
                Publisher = publisher,
                CreatedBy = user.Id,
                CreatedOn = (DateTime)solution[0]["createdon"],
                ModifiedBy = user.Id,
                ModifiedOn = (DateTime)solution[0]["modifiedon"]
            };

            var existingApplications = this.dbContext.Applications.Where(e => e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
            var existingMaxOrdinalNumber = existingApplications.Max(e => e.OrdinalNumber);
            application.OrdinalNumber = existingMaxOrdinalNumber != null ? existingMaxOrdinalNumber + 1 : 1;

            dbContext.Add(application);
            await dbContext.SaveChangesAsync();

            // Create Upgrade
            string version = (string)solution[0]["version"];
            await CreateNewUpgrade(application, version, solutionService);

            return Ok();
        }

        
        [HttpPost]
        public async Task<int> GetDeploymentSettingsStatus([FromODataUri] int key, ODataActionParameters parameters, [FromServices] ConnectionReferenceService connectionReferenceService, [FromServices] EnvironmentVariableService environmentVariableService)
        {
            //status 0=incomplete configuration;1=complete configuration
            int environmentId = (int)parameters["environmentId"];
            var statusConnectionReferences = await connectionReferenceService.GetStatus(key, environmentId);
            var statusEnvironmentVariables = await environmentVariableService.GetStatus(key, environmentId);

            if(statusConnectionReferences == 0 || statusEnvironmentVariables == 0)
                return 0;

            return 1;
        }

        private bool ApplicationExists(int key)
        {
            return base.dbContext.Applications.Any(p => p.Id == key);
        }

        private async Task CreateSolutionInDataverse(Application application)
        {
            string environmentUrl = this.dbContext.Environments.First(x => x.Id == application.DevelopmentEnvironment).BasicUrl;

            JObject newSolution = new JObject();
            //newSolution.Add("solutionid", $"{application.MsId}");
            newSolution.Add("ismanaged", "false");
            newSolution.Add("uniquename", $"{application.SolutionUniqueName}");
            newSolution.Add("friendlyname", $"{application.Name}");
            newSolution.Add("version", "1.0.0.0");
            newSolution.Add("publisherid@odata.bind", $"/publishers({application.PublisherNavigation.MsId})");

            StringContent solutionContent = new StringContent(JsonConvert.SerializeObject(newSolution), Encoding.UTF8, mediaType: "application/json");

            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{this.msIdTenantCurrentUser}";
                options.BaseUrl = environmentUrl + options.BaseUrl;
                options.RelativePath = "/solutions";
                options.HttpMethod = HttpMethod.Post;
                options.Scopes = $"{environmentUrl}/.default";
            }, content: solutionContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not create Solution in Dataverse");
            }

        }

        private async Task CreateNewUpgrade(Application application, string version, SolutionService solutionService)
        {

            Upgrade upgrade = new Upgrade
            {
                Name = "Initial Upgrade",
                Application = application.Id,
                ApplyManually = false
            };

            await solutionService.CreateUpgrade(upgrade, version);
            this.dbContext.Upgrades.Add(upgrade);
            await this.dbContext.SaveChangesAsync();
        }

        private async Task<Publisher> CreateNewPublisher(Guid newPublisherGuid, User user, at.D365.PowerCID.Portal.Data.Models.Environment environment)
        {
            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{user.TenantNavigation.MsId}";
                options.BaseUrl = environment.BasicUrl + options.BaseUrl;
                options.RelativePath = $"/publishers({newPublisherGuid})";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{environment.BasicUrl}/.default";
            });

            JObject responseData = await response.Content.ReadAsAsync<JObject>();

            Publisher newPublisher = new Publisher
            {
                Name = (string)responseData["friendlyname"],
                MsId = (Guid)responseData["publisherid"],
                Environment = environment.Id
            };

            return newPublisher;
        }
    }
}