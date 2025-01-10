using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.OData;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class UpgradesController : BaseController
    {
        private readonly ILogger logger;

        public UpgradesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<UpgradesController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        //GET: odata/Upgrades
        [EnableQuery]
        public IQueryable<Upgrade> Get()
        {
            logger.LogDebug($"Begin & End: UpgradesController Get()");

            return base.dbContext.Upgrades.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdCurrentUser);
        }

        public async Task<IActionResult> Post([FromBody] Upgrade upgrade, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: UpgradesController Post(upgrade Application: {upgrade.Application})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((await this.dbContext.Applications.FirstOrDefaultAsync(e => e.Id == upgrade.Application && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            await solutionService.CreateUpgrade(upgrade, version: null);

            this.dbContext.Upgrades.Add(upgrade);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"Begin: UpgradesController Post(upgrade Application: {upgrade.Application})");

            return Created(upgrade);
        }

        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Upgrade> upgrade)
        {
            logger.LogDebug($"Begin: UpgradesController Patch(key: {key}, upgrade: {upgrade.GetChangedPropertyNames().ToString()}");

            if ((await this.dbContext.Upgrades.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            string[] propertyNamesAllowedToChange = { nameof(Solution.Name), nameof(Solution.Description) };
            if (upgrade.GetChangedPropertyNames().Except(propertyNamesAllowedToChange).Count() == 0)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var entity = await base.dbContext.Upgrades.FindAsync(key);
                if (entity == null)
                {
                    return NotFound();
                }

                if(upgrade.GetChangedPropertyNames().Contains(nameof(Solution.Name)) && entity.Actions.Any(e => e.Result == 1 || e.Status == 2 || e.Status == 1))
                    return BadRequest(new ODataError { ErrorCode =  "400", Message = "Can not rename Upgrade with existing Actions in progress or successfully completed." });

                upgrade.Patch(entity);
                try
                {
                    await base.dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UpgradeExists(key))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                logger.LogDebug($"End: UpgradesController Patch(key: {key}, upgrade: {upgrade.GetChangedPropertyNames().ToString()}");

                return Updated(entity);
            }
            else
            {
                return BadRequest();
            }
        }

        private bool UpgradeExists(int key)
        {
            logger.LogDebug($"Begin & End: UpgradesController UpgradeExists(key: {key})");

            return base.dbContext.Upgrades.Any(p => p.Id == key);
        }
    }
}
