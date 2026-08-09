using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Controllers
{
    /// <summary>
    /// Maps an <see cref="Application"/> to the <see cref="ExternalDeploymentPath"/>(s) it can be delivered through.
    /// Mirrors <see cref="ApplicationDeploymentPathsController"/> for the external deployment area. Only available to
    /// the vendor tenant.
    /// </summary>
    [Authorize(Roles = "atPowerCID.Admin, atPowerCID.ExternalReleaseManager")]
    [CrossTenantDeliveryGate]
    public class ApplicationExternalDeploymentPathsController : BaseController
    {
        private readonly ILogger logger;

        public ApplicationExternalDeploymentPathsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ApplicationExternalDeploymentPathsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<ApplicationExternalDeploymentPath> Get()
        {
            logger.LogDebug("Begin: ApplicationExternalDeploymentPathsController Get()");

            return base.dbContext.ApplicationExternalDeploymentPaths.OrderBy(ad => ad.HierarchieNumber).Where(e => e.ExternalDeploymentPathNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyApplication, [FromODataUri] int keyExternalDeploymentPath)
        {
            logger.LogDebug($"Begin: ApplicationExternalDeploymentPathsController Delete(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            var applicationExternalDeploymentPathToDelete = this.dbContext.ApplicationExternalDeploymentPaths.FirstOrDefault(e => e.Application == keyApplication && e.ExternalDeploymentPath == keyExternalDeploymentPath && e.ExternalDeploymentPathNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);

            if (applicationExternalDeploymentPathToDelete == null)
                return NotFound();

            if (applicationExternalDeploymentPathToDelete.HierarchieNumber == null)
                return BadRequest("HierarchieNumber is required for this assignment.");

            SortWhenRemoved(keyApplication, applicationExternalDeploymentPathToDelete.HierarchieNumber.Value.ToString());
            this.dbContext.Remove(applicationExternalDeploymentPathToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ApplicationExternalDeploymentPathsController Delete(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApplicationExternalDeploymentPath applicationExternalDeploymentPath)
        {
            logger.LogDebug($"Begin: ApplicationExternalDeploymentPathsController Post(applicationExternalDeploymentPath: {applicationExternalDeploymentPath.ExternalDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (this.dbContext.Applications.FirstOrDefault(e => e.Id == applicationExternalDeploymentPath.Application && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser) == null)
                return Forbid();

            if (this.dbContext.ExternalDeploymentPaths.FirstOrDefault(e => e.Id == applicationExternalDeploymentPath.ExternalDeploymentPath && e.TenantNavigation.MsId == this.msIdTenantCurrentUser) == null)
                return Forbid();

            if (dbContext.ApplicationExternalDeploymentPaths.Any(x => x.Application == applicationExternalDeploymentPath.Application && x.ExternalDeploymentPath == applicationExternalDeploymentPath.ExternalDeploymentPath))
            {
                return BadRequest("No changes");
            }

            SortWhenAdded(applicationExternalDeploymentPath.Application, applicationExternalDeploymentPath.HierarchieNumber.ToString());
            base.dbContext.ApplicationExternalDeploymentPaths.Add(applicationExternalDeploymentPath);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ApplicationExternalDeploymentPathsController Post(applicationExternalDeploymentPath: {applicationExternalDeploymentPath.ExternalDeploymentPath})");

            return Created(applicationExternalDeploymentPath);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyApplication, [FromODataUri] int keyExternalDeploymentPath, [FromBody] object parameters)
        {
            logger.LogDebug($"Begin: ApplicationExternalDeploymentPathsController Patch(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (this.dbContext.Applications.FirstOrDefault(e => e.Id == keyApplication && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser) == null)
                return Forbid();

            if (this.dbContext.ExternalDeploymentPaths.FirstOrDefault(e => e.Id == keyExternalDeploymentPath && e.TenantNavigation.MsId == this.msIdTenantCurrentUser) == null)
                return Forbid();

            var parametersAsJObject = parameters == null ? null : JsonConvert.DeserializeObject<JObject>(parameters.ToString());
            if (parametersAsJObject == null ||
                !int.TryParse(parametersAsJObject["FromIndex"]?.ToString(), out int fromIndex) ||
                !int.TryParse(parametersAsJObject["ToIndex"]?.ToString(), out int toIndex))
                return BadRequest("FromIndex and ToIndex are required.");

            ApplicationExternalDeploymentPath applicationExternalDeploymentPathFromIndex = dbContext.ApplicationExternalDeploymentPaths.FirstOrDefault(x =>
                x.Application == keyApplication &&
                x.ExternalDeploymentPath == keyExternalDeploymentPath &&
                x.ExternalDeploymentPathNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
            if (applicationExternalDeploymentPathFromIndex == null)
                return NotFound();

            if (fromIndex != toIndex)
                SortWhenUpdated(keyApplication, fromIndex, toIndex);
            applicationExternalDeploymentPathFromIndex.HierarchieNumber = toIndex;

            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ApplicationExternalDeploymentPathsController Patch(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            return Updated(applicationExternalDeploymentPathFromIndex);
        }

        private void SortWhenUpdated(int applicationId, int fromIndex, int toIndex)
        {
            if (fromIndex < toIndex)
            {
                var pathsInBetween = dbContext.ApplicationExternalDeploymentPaths.Where(x => x.Application == applicationId && x.HierarchieNumber > fromIndex && x.HierarchieNumber <= toIndex);

                foreach (var path in pathsInBetween)
                    path.HierarchieNumber = path.HierarchieNumber - 1;
            }
            else
            {
                var pathsInBetween = dbContext.ApplicationExternalDeploymentPaths.Where(x => x.Application == applicationId && x.HierarchieNumber < fromIndex && x.HierarchieNumber >= toIndex);

                foreach (var path in pathsInBetween)
                    path.HierarchieNumber = path.HierarchieNumber + 1;
            }
        }

        private void SortWhenRemoved(int applicationId, string hierarchieNumber)
        {
            var pathsWithHigherNumber = dbContext.ApplicationExternalDeploymentPaths.Where(x => x.HierarchieNumber > int.Parse(hierarchieNumber) && x.Application == applicationId);

            foreach (var path in pathsWithHigherNumber)
                path.HierarchieNumber = path.HierarchieNumber - 1;
        }

        private void SortWhenAdded(int applicationId, string hierarchieNumber)
        {
            var pathsWithHigherNumber = dbContext.ApplicationExternalDeploymentPaths.Where(x => x.HierarchieNumber >= int.Parse(hierarchieNumber) && x.Application == applicationId);

            foreach (var path in pathsWithHigherNumber)
                path.HierarchieNumber = path.HierarchieNumber + 1;
        }
    }
}
