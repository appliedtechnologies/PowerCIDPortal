using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class AsyncJobsController : BaseController
    {
        public AsyncJobsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [EnableQuery]
        public IQueryable<AsyncJob> Get([FromODataUri] int key)
        {
            return base.dbContext.AsyncJobs.Where(e => e.ActionNavigation.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser && e.Id == key);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        // GET: odata/Actions
        [EnableQuery]
        public IQueryable<AsyncJob> Get()
        {
            return base.dbContext.AsyncJobs.Where(e => e.ActionNavigation.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            var asyncJob = await dbContext.AsyncJobs.FindAsync(key);

            if((await this.dbContext.AsyncJobs.FirstOrDefaultAsync(e => e.Id == key && e.ActionNavigation.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            if (asyncJob == null)
            {
                return NotFound();
            }
            dbContext.AsyncJobs.Remove(asyncJob);
            await dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
