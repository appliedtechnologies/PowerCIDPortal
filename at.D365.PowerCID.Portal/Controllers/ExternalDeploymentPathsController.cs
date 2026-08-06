using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OData;

namespace at.D365.PowerCID.Portal.Controllers
{
    /// <summary>
    /// Manages deployment paths used to ship externally released solutions into foreign tenants. Mirrors
    /// <see cref="DeploymentPathsController"/> for the external delivery area. Only available to the vendor tenant.
    /// </summary>
    [Authorize(Roles = "atPowerCID.Admin, atPowerCID.ExternalReleaseManager")]
    [CrossTenantDeliveryGate]
    public class ExternalDeploymentPathsController : BaseController
    {
        private readonly ILogger logger;

        public ExternalDeploymentPathsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ExternalDeploymentPathsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        // GET: odata/ExternalDeploymentPaths
        [EnableQuery]
        public IQueryable<ExternalDeploymentPath> Get()
        {
            logger.LogDebug("Begin & End: ExternalDeploymentPathsController Get()");

            return base.dbContext.ExternalDeploymentPaths.Where(e => e.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ExternalDeploymentPath externalDeploymentPath)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathsController Post(externalDeploymentPath Name: {externalDeploymentPath.Name})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dbContext.ExternalDeploymentPaths.Any(x => x.TenantNavigation.MsId == this.msIdTenantCurrentUser && x.Name == externalDeploymentPath.Name))
                return BadRequest(new ODataError { Code = "400", Message = "An external deployment path with this name already exists." });

            externalDeploymentPath.Tenant = this.dbContext.Tenants.First(e => e.MsId == this.msIdTenantCurrentUser).Id;
            base.dbContext.ExternalDeploymentPaths.Add(externalDeploymentPath);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ExternalDeploymentPathsController Post(externalDeploymentPath Name: {externalDeploymentPath.Name})");

            return Created(externalDeploymentPath);
        }

        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<ExternalDeploymentPath> externalDeploymentPath)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathsController Patch(key: {key}, externalDeploymentPath: {externalDeploymentPath.GetChangedPropertyNames()})");

            if ((await this.dbContext.ExternalDeploymentPaths.FirstOrDefaultAsync(e => e.Id == key && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            string[] propertyNamesAllowedToChange = { nameof(ExternalDeploymentPath.Name) };
            if (externalDeploymentPath.GetChangedPropertyNames().Except(propertyNamesAllowedToChange).Any())
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await base.dbContext.ExternalDeploymentPaths.FindAsync(key);
            if (entity == null)
                return NotFound();

            externalDeploymentPath.Patch(entity);

            if (dbContext.ExternalDeploymentPaths.Any(x => x.TenantNavigation.MsId == this.msIdTenantCurrentUser && x.Name == entity.Name))
                return BadRequest(new ODataError { Code = "400", Message = "An external deployment path with this name already exists." });

            try
            {
                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExternalDeploymentPathExists(key))
                    return NotFound();
                else
                    throw;
            }

            logger.LogDebug($"End: ExternalDeploymentPathsController Patch(key: {key})");

            return Updated(entity);
        }

        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathsController Delete(key: {key})");

            var externalDeploymentPathToDelete = await this.dbContext.ExternalDeploymentPaths.FindAsync(key);

            if (externalDeploymentPathToDelete == null)
                return NotFound();

            if ((await this.dbContext.Tenants.FirstOrDefaultAsync(e => e.Id == externalDeploymentPathToDelete.Tenant && e.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            if (this.dbContext.ApplicationExternalDeploymentPaths.Any(e => e.ExternalDeploymentPath == key))
                return BadRequest(new ODataError { Code = "400", Message = $"The external deployment path is used by application '{this.dbContext.ApplicationExternalDeploymentPaths.First(e => e.ExternalDeploymentPath == key).ApplicationNavigation.Name}'." });

            this.dbContext.Remove(externalDeploymentPathToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ExternalDeploymentPathsController Delete(key: {key})");

            return Ok();
        }

        private bool ExternalDeploymentPathExists(int key)
        {
            logger.LogDebug($"Begin & End: ExternalDeploymentPathsController ExternalDeploymentPathExists(key: {key})");

            return base.dbContext.ExternalDeploymentPaths.Any(p => p.Id == key);
        }
    }
}
