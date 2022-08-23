using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Identity.Web;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class DeploymentPathsController : BaseController
    {
        public DeploymentPathsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ActionStatusController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        [EnableQuery]
        public IQueryable<DeploymentPath> Get()
        {
            return base.dbContext.DeploymentPaths.Where(e => e.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DeploymentPath deploymentPath)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (dbContext.DeploymentPaths.Any(x => x.Name == deploymentPath.Name))
            {
                return BadRequest("Deploymentpath already exists with this name");
            }

            deploymentPath.Tenant = this.dbContext.Tenants.First(e => e.MsId == this.msIdTenantCurrentUser).Id;
            base.dbContext.DeploymentPaths.Add(deploymentPath);
            await base.dbContext.SaveChangesAsync();

            return Created(deploymentPath);

        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        public async Task<IActionResult> Delete([FromODataUri] int key)
        {
            var deploymentPathToDelete = await this.dbContext.DeploymentPaths.FindAsync(key);

            if (deploymentPathToDelete == null)
                return NotFound();

            if((await this.dbContext.Tenants.FirstOrDefaultAsync(e => e.Id == deploymentPathToDelete.Tenant && e.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            this.dbContext.Remove(deploymentPathToDelete);
            await this.dbContext.SaveChangesAsync();
            return Ok();
        }

        private bool DeploymentpathExists(int key)
        {
            return base.dbContext.DeploymentPaths.Any(p => p.Id == key);
        }

        private bool EnvironmentExists(int key)
        {
            return base.dbContext.Environments.Any(p => p.Id == key);
        }        
    }
}