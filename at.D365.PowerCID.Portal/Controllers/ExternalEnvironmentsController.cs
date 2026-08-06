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
    /// Manages the registration of existing <see cref="Data.Models.Environment"/>s (typically belonging to
    /// foreign/customer tenants) as valid targets for cross-tenant solution delivery. Only available to the
    /// vendor tenant configured via "CrossTenantDelivery:AllowedTenantId".
    /// </summary>
    [Authorize(Roles = "atPowerCID.Admin, atPowerCID.ExternalReleaseManager")]
    [CrossTenantDeliveryGate]
    public class ExternalEnvironmentsController : BaseController
    {
        private readonly ILogger logger;

        public ExternalEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ExternalEnvironmentsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        // GET: odata/ExternalEnvironments
        [EnableQuery]
        public IQueryable<ExternalEnvironment> Get()
        {
            logger.LogDebug("Begin & End: ExternalEnvironmentsController Get()");

            return base.dbContext.ExternalEnvironments;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ExternalEnvironment externalEnvironment)
        {
            logger.LogDebug($"Begin: ExternalEnvironmentsController Post(externalEnvironment Environment: {externalEnvironment.Environment})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await this.dbContext.Environments.AnyAsync(e => e.Id == externalEnvironment.Environment))
                return BadRequest(new ODataError { Code = "400", Message = "The referenced environment does not exist." });

            if (await this.dbContext.ExternalEnvironments.AnyAsync(e => e.Environment == externalEnvironment.Environment))
                return BadRequest(new ODataError { Code = "400", Message = "This environment is already registered for external delivery." });

            base.dbContext.ExternalEnvironments.Add(externalEnvironment);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ExternalEnvironmentsController Post(externalEnvironment Environment: {externalEnvironment.Environment})");

            return Created(externalEnvironment);
        }

        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<ExternalEnvironment> externalEnvironment)
        {
            logger.LogDebug($"Begin: ExternalEnvironmentsController Patch(key: {key}, externalEnvironment: {externalEnvironment.GetChangedPropertyNames()})");

            string[] propertyNamesAllowedToChange = { nameof(ExternalEnvironment.Alias), nameof(ExternalEnvironment.IsDeactive) };
            if (externalEnvironment.GetChangedPropertyNames().Except(propertyNamesAllowedToChange).Any())
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await base.dbContext.ExternalEnvironments.FindAsync(key);
            if (entity == null)
                return NotFound();

            bool wasDeactive = entity.IsDeactive;
            externalEnvironment.Patch(entity);

            string pathName = await this.dbContext.ExternalDeploymentPathEnvironments
                .Where(e => e.ExternalEnvironment == key)
                .Select(e => e.ExternalDeploymentPathNavigation.Name)
                .FirstOrDefaultAsync();

            if (!wasDeactive && entity.IsDeactive && pathName != null)
                return BadRequest(new ODataError { Code = "400", Message = $"The external environment is used in external deployment path '{pathName}'." });

            try
            {
                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExternalEnvironmentExists(key))
                    return NotFound();
                else
                    throw;
            }

            logger.LogDebug($"End: ExternalEnvironmentsController Patch(key: {key})");

            return Updated(entity);
        }

        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            logger.LogDebug($"Begin: ExternalEnvironmentsController Delete(key: {key})");

            var externalEnvironmentToDelete = await this.dbContext.ExternalEnvironments.FindAsync(key);
            if (externalEnvironmentToDelete == null)
                return NotFound();

            if (this.dbContext.ExternalDeploymentPathEnvironments.Any(e => e.ExternalEnvironment == key))
                return BadRequest(new ODataError { Code = "400", Message = $"The external environment is used in external deployment path '{this.dbContext.ExternalDeploymentPathEnvironments.First(e => e.ExternalEnvironment == key).ExternalDeploymentPathNavigation.Name}'." });

            this.dbContext.Remove(externalEnvironmentToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ExternalEnvironmentsController Delete(key: {key})");

            return Ok();
        }

        /// <summary>
        /// Deliberate, tightly-scoped exception to normal tenant isolation: lists environments across ALL tenants
        /// (excluding ones already registered) so the vendor tenant can pick a customer's environment to register.
        /// Only Admin/ExternalReleaseManager of the vendor tenant can reach this (see class-level attributes).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetRegistrableEnvironments()
        {
            logger.LogDebug("Begin: ExternalEnvironmentsController GetRegistrableEnvironments()");

            var registrableEnvironments = await this.dbContext.Environments
                .Where(e => !e.IsDeactive && !this.dbContext.ExternalEnvironments.Any(ee => ee.Environment == e.Id))
                .OrderBy(e => e.TenantNavigation.Name).ThenBy(e => e.Name)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.BasicUrl,
                    TenantId = e.Tenant,
                    TenantName = e.TenantNavigation.Name,
                    TenantMsId = e.TenantNavigation.MsId
                })
                .ToListAsync();

            logger.LogDebug($"End: ExternalEnvironmentsController GetRegistrableEnvironments(Count: {registrableEnvironments.Count})");

            return Ok(registrableEnvironments);
        }

        private bool ExternalEnvironmentExists(int key)
        {
            logger.LogDebug($"Begin & End: ExternalEnvironmentsController ExternalEnvironmentExists(key: {key})");

            return base.dbContext.ExternalEnvironments.Any(p => p.Id == key);
        }
    }
}
