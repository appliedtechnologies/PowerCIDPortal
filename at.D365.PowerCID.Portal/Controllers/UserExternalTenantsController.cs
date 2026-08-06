using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OData;

namespace at.D365.PowerCID.Portal.Controllers
{
    /// <summary>
    /// Grants users of the vendor tenant permission to deploy externally released solutions into a specific
    /// foreign (customer) tenant. Mirrors <see cref="UserEnvironmentsController"/>, which grants per-environment
    /// deploy permission for internal deliveries. Only available to the vendor tenant.
    /// </summary>
    [Authorize(Roles = "atPowerCID.Admin")]
    [CrossTenantDeliveryGate]
    public class UserExternalTenantsController : BaseController
    {
        private readonly ILogger logger;

        public UserExternalTenantsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<UserExternalTenantsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<UserExternalTenant> Get()
        {
            logger.LogDebug("Begin & End: UserExternalTenantsController Get()");

            return base.dbContext.UserExternalTenants.Where(e => e.UserNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        public async Task<IActionResult> Post([FromBody] UserExternalTenant userExternalTenant)
        {
            logger.LogDebug($"Begin: UserExternalTenantsController Post(userExternalTenant Tenant: {userExternalTenant.Tenant})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if ((await this.dbContext.Users.FirstOrDefaultAsync(u => u.Id == userExternalTenant.User && u.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            Tenant targetTenant = await this.dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == userExternalTenant.Tenant);
            if (targetTenant == null)
                return NotFound();

            if (targetTenant.MsId == this.msIdTenantCurrentUser)
                return BadRequest(new ODataError { Code = "400", Message = "Can't grant an external delivery permission for your own tenant." });

            base.dbContext.UserExternalTenants.Add(userExternalTenant);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: UserExternalTenantsController Post(userExternalTenant Tenant: {userExternalTenant.Tenant})");

            return Created(userExternalTenant);
        }

        [HttpDelete("odata/UserExternalTenants(User={keyUser},Tenant={keyTenant})")]
        public async Task<IActionResult> Delete([FromODataUri] int keyUser, [FromODataUri] int keyTenant)
        {
            logger.LogDebug($"Begin: UserExternalTenantsController Delete(keyUser: {keyUser}, keyTenant: {keyTenant})");

            if ((await this.dbContext.Users.FirstOrDefaultAsync(u => u.Id == keyUser && u.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var userExternalTenantToDelete = await this.dbContext.UserExternalTenants.FindAsync(keyUser, keyTenant);

            if (userExternalTenantToDelete == null)
                return NotFound();

            this.dbContext.Remove(userExternalTenantToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: UserExternalTenantsController Delete(keyUser: {keyUser}, keyTenant: {keyTenant})");

            return Ok();
        }
    }
}
