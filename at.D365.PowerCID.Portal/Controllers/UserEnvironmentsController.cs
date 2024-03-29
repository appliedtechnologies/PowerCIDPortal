using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Logging;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class UserEnvironmentsController : BaseController
    {
        private readonly ILogger logger;

        public UserEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ActionStatusController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<UserEnvironment> Get()
        {
            logger.LogDebug($"Begin & End: UserEnvironmentsController Get()");

            return base.dbContext.UserEnvironments.Where(e => e.EnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Post([FromBody] UserEnvironment userEnvironment)
        {
            logger.LogDebug($"Begin: UserEnvironmentsController Post(userEnvironment Environment: {userEnvironment.Environment})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == userEnvironment.Environment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            base.dbContext.UserEnvironments.Add(userEnvironment);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: UserEnvironmentsController Post(userEnvironment Environment: {userEnvironment.Environment})");

            return Created(userEnvironment);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpDelete("odata/UserEnvironments(User={keyUser},Environment={keyEnvironment})")]
        public async Task<IActionResult> Delete([FromODataUri] int keyUser, [FromODataUri] int keyEnvironment)
        {
            logger.LogDebug($"Begin: UserEnvironmentsController Delete(keyUser: {keyUser}, keyEnvironment: {keyEnvironment})");

            if ((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == keyEnvironment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var userEnvironmentToDelete = await this.dbContext.UserEnvironments.FindAsync(keyUser, keyEnvironment);

            if (userEnvironmentToDelete == null)
                return NotFound();

            this.dbContext.Remove(userEnvironmentToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: UserEnvironmentsController Delete(keyUser: {keyUser}, keyEnvironment: {keyEnvironment})");

            return Ok();
        }
    }
}
