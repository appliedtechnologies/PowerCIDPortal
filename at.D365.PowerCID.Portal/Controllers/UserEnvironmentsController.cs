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

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class UserEnvironmentsController : BaseController
    {
        public UserEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        [EnableQuery]
        public IQueryable<UserEnvironment> Get()
        {
            return base.dbContext.UserEnvironments.Where(e => e.EnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Post([FromBody] UserEnvironment userEnvironment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == userEnvironment.Environment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            base.dbContext.UserEnvironments.Add(userEnvironment);
            await base.dbContext.SaveChangesAsync();

            return Created(userEnvironment);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpDelete("odata/UserEnvironments(User={keyUser},Environment={keyEnvironment})")]
        public async Task<IActionResult> Delete([FromODataUri] int keyUser, [FromODataUri] int keyEnvironment)
        {
            if((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == keyEnvironment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var userEnvironmentToDelete = await this.dbContext.UserEnvironments.FindAsync(keyUser, keyEnvironment);

            if(userEnvironmentToDelete == null)
                return NotFound();

            this.dbContext.Remove(userEnvironmentToDelete);
            await this.dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
