using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using at.D365.PowerCID.Portal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class UpgradesController : BaseController
    {


        public UpgradesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        //GET: odata/Upgrades
        [EnableQuery]
        public IQueryable<Upgrade> Get()
        {
            return base.dbContext.Upgrades.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdCurrentUser);
        }

        public async Task<IActionResult> Post([FromBody] Upgrade upgrade, [FromServices] SolutionService solutionService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if((await this.dbContext.Applications.FirstOrDefaultAsync(e => e.Id == upgrade.Application && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            await solutionService.CreateUpgrade(upgrade, version: null);

            this.dbContext.Upgrades.Add(upgrade);
            await this.dbContext.SaveChangesAsync();

            return Created(upgrade);
        }
    }
}
