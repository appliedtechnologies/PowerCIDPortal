using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class ApplicationDeploymentPathsController : BaseController
    {
        private readonly ILogger logger;
        public ApplicationDeploymentPathsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ApplicationDeploymentPathsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<ApplicationDeploymentPath> Get()
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Get()");

            return base.dbContext.ApplicationDeploymentPaths.OrderBy(ad => ad.HierarchieNumber).Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyApplication, [FromODataUri] int keyDeploymentPath)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Delete(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            var applicationDeploymentPathToDelete = this.dbContext.ApplicationDeploymentPaths.FirstOrDefault(e => (e.Application == keyApplication && e.DeploymentPath == keyDeploymentPath) && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);

            if (applicationDeploymentPathToDelete == null)
                return NotFound();

            sortWhenRemoved(keyApplication, keyDeploymentPath, applicationDeploymentPathToDelete.HierarchieNumber.ToString());

            this.dbContext.Remove(applicationDeploymentPathToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ApplicationDeploymentPathsController Delete(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            return Ok();
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApplicationDeploymentPath applicationDeploymentPath)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Post(applicationDeploymentPath: {applicationDeploymentPath.DeploymentPath})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (this.dbContext.Applications.First(e => e.Id == applicationDeploymentPath.Application && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser) == null)
            {
                return Forbid();
            }

            if (dbContext.ApplicationDeploymentPaths.Any(x => x.Application == applicationDeploymentPath.Application && x.DeploymentPath == applicationDeploymentPath.DeploymentPath))
            {
                return BadRequest("No changes");
            }
            else
            {
                sortWhenAdded(applicationDeploymentPath.Application, applicationDeploymentPath.DeploymentPath, applicationDeploymentPath.HierarchieNumber.ToString());
                base.dbContext.ApplicationDeploymentPaths.Add(applicationDeploymentPath);
                await base.dbContext.SaveChangesAsync();

                logger.LogDebug($"End: ApplicationDeploymentPathsController Post(applicationDeploymentPath: {applicationDeploymentPath.DeploymentPath})");

                return Created(applicationDeploymentPath);
            }
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyApplication, [FromODataUri] int keyDeploymentPath, [FromBody] Object parameters)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Patch(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (this.dbContext.Applications.FirstOrDefault(e => e.Id == keyApplication && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser) == null)
                return Forbid();

            var parametersAsJObject = JsonConvert.DeserializeObject<JObject>(parameters.ToString());
            int fromIndex = (int)parametersAsJObject["FromIndex"];
            int toIndex = (int)parametersAsJObject["ToIndex"];

            sortWhenUpdated(keyApplication, fromIndex, toIndex);

            ApplicationDeploymentPath applicationDeploymentPathFromIndex = dbContext.ApplicationDeploymentPaths.FirstOrDefault(x => x.Application == keyApplication && x.DeploymentPath == keyDeploymentPath);
            applicationDeploymentPathFromIndex.HierarchieNumber = toIndex;

            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ApplicationDeploymentPathsController Patch(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            return Updated(applicationDeploymentPathFromIndex);
        }

        private void sortWhenUpdated(int applicationId, int fromIndex, int toIndex)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController sortWhenUpdated(applicationId: {applicationId}, fromIndex: {fromIndex}, toIndex:{toIndex})");

            if (fromIndex < toIndex)
            {
                var ApplicationDeploymentPathsInBetween = dbContext.ApplicationDeploymentPaths.Where(x => x.Application == applicationId && x.HierarchieNumber > fromIndex && x.HierarchieNumber <= toIndex);

                foreach (var application in ApplicationDeploymentPathsInBetween)
                {
                    application.HierarchieNumber = application.HierarchieNumber - 1;
                }
                logger.LogDebug($"End: ApplicationDeploymentPathsController sortWhenUpdated(applicationId: {applicationId}, fromIndex: {fromIndex}, toIndex:{toIndex})");
            }
            else
            {
                var ApplicationDeploymentPathsInBetween = dbContext.ApplicationDeploymentPaths.Where(x => x.Application == applicationId && x.HierarchieNumber < fromIndex && x.HierarchieNumber >= toIndex);

                foreach (var application in ApplicationDeploymentPathsInBetween)
                {
                    application.HierarchieNumber = application.HierarchieNumber + 1;
                }
                logger.LogDebug($"End: ApplicationDeploymentPathsController sortWhenUpdated(applicationId: {applicationId}, fromIndex: {fromIndex}, toIndex:{toIndex})");
            }
        }


        private void sortWhenRemoved(int applicationId, int deploymentPathId, string hierarchieNumber)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController sortWhenRemoved(applicationId: {applicationId}, deploymentPathId: {deploymentPathId}, hierarchieNumber: {hierarchieNumber})");

            var ApplicationDeploymentPathsWithHigherNumber = dbContext.ApplicationDeploymentPaths.Where(x => x.HierarchieNumber > int.Parse(hierarchieNumber) && x.Application == applicationId);

            foreach (var application in ApplicationDeploymentPathsWithHigherNumber)
            {
                application.HierarchieNumber = application.HierarchieNumber - 1;
            }
            logger.LogDebug($"End: ApplicationDeploymentPathsController sortWhenRemoved(applicationId: {applicationId}, deploymentPathId: {deploymentPathId}, hierarchieNumber: {hierarchieNumber})");
        }
        private void sortWhenAdded(int applicationId, int deploymentPathId, string hierarchieNumber)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController sortWhenAdded(applicationId: {applicationId}, deploymentPathId: {deploymentPathId}, hierarchieNumber: {hierarchieNumber})");

            var ApplicationDeploymentPathsWithHigherNumber = dbContext.ApplicationDeploymentPaths.Where(x => x.HierarchieNumber >= int.Parse(hierarchieNumber) && x.Application == applicationId);

            foreach (var application in ApplicationDeploymentPathsWithHigherNumber)
            {
                application.HierarchieNumber = application.HierarchieNumber + 1;
            }
            logger.LogDebug($"End: ApplicationDeploymentPathsController sortWhenRemoved(applicationId: {applicationId}, deploymentPathId: {deploymentPathId}, hierarchieNumber: {hierarchieNumber})");
        }
    }
}
