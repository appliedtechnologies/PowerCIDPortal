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
    public class ActionsController : BaseController
    {
        public ActionsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        [EnableQuery]
        public IQueryable<Action> Get([FromODataUri] int key)
        {
            return base.dbContext.Actions.Where(e => e.TargetEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser && e.Id == key);
        }

        // GET: odata/Actions
        [EnableQuery]
        public IQueryable<at.D365.PowerCID.Portal.Data.Models.Action> Get()
        {
            return base.dbContext.Actions.Where(e => e.TargetEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Action> action)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if((await this.dbContext.Actions.FirstOrDefaultAsync(e => e.Id == key && e.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var entity = await base.dbContext.Actions.FindAsync(key);
            if (entity == null)
            {
                return NotFound();
            }
            action.Patch(entity);
            try
            {
                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActionExists(key))
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

        [HttpPost]
        public async Task<IActionResult> CancelImport([FromODataUri] int key)
        {
            var action = await this.dbContext.Actions.FirstOrDefaultAsync(e => e.Id == key && e.SolutionNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
            if(action == null)
                return Forbid();

            if(action.Status == 3 || action.Type == 1)
                return BadRequest();

            if(action.AsyncJobs.Count > 0)
                this.dbContext.RemoveRange(action.AsyncJobs);

            action.Status = 3;
            action.Result = 2;
            action.FinishTime = System.DateTime.Now;
            action.ErrorMessage = "canceled manually in Power CID Portal - no effect on any operations that may already have started on the environment";

            await this.dbContext.SaveChangesAsync();            

            return Ok();
        }

        private bool ActionExists(int key)
        {
            return base.dbContext.Applications.Any(p => p.Id == key);
        }
    }
}
