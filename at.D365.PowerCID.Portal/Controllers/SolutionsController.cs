using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using at.D365.PowerCID.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class SolutionsController : BaseController
    {

        public SolutionsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        [EnableQuery]
        public IQueryable<Solution> Get()
        {
            return base.dbContext.Solutions.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        
        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Solution> solution)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var entity = await base.dbContext.Solutions.FindAsync(key);
            if (entity == null)
            {
                return NotFound();
            }
            solution.Patch(entity);
            try
            {
                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SolutionExists(key))
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
        public async Task<IActionResult> GetSolutionAsBase64String([FromODataUri] int key, [FromServices] SolutionService solutionService)
        {
            if((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();
            
            Solution solution = dbContext.Solutions.FirstOrDefault(x => x.Id == key);
            Tenant tenant = dbContext.Solutions.FirstOrDefault(x => x.Id == key).ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation;

            var solutionAsBase64String = await solutionService.GetSolutionFromGitHubAsBase64String(tenant, solution);
            return Ok(solutionAsBase64String);
        }

        [HttpPost]
        public async Task<IActionResult> Export([FromODataUri] int key, [FromServices] SolutionService solutionService)
        {
            if((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            Data.Models.Action createdAction = await solutionService.Export(key, this.msIdTenantCurrentUser, this.msIdCurrentUser, exportOnly: true);
            return Ok(createdAction);
        }

        [HttpPost]
        public async Task<IActionResult> Import([FromODataUri] int key, ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            if((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            int targetEnvironmentId = (int)parameters["targetEnvironmentId"];
            int deploymentPathId = (int)parameters["deploymentPathId"];

            int userId = dbContext.Users.FirstOrDefault(u => u.MsId == this.msIdCurrentUser).Id;

            if (IsPreviousDeploymentEnvironmentResultSuccessful(deploymentPathId, targetEnvironmentId, key) == false)
            {
                return BadRequest("Cant skip a previous deploymentenvironment");
            }

            else
            {
                Data.Models.Action createdAction;

                try
                {
                    if (ExportExists(key))
                        createdAction = await solutionService.Import(key, targetEnvironmentId, this.msIdCurrentUser);
                    else
                        createdAction = await solutionService.Export(key, this.msIdTenantCurrentUser, this.msIdCurrentUser, exportOnly: false, targetEnvironmentId);
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }

                return Ok(createdAction);
            }
        }

        private bool IsPreviousDeploymentEnvironmentResultSuccessful(int deploymentPathId, int targetEnvironmentId, int solutionId)
        {
            int stepNumber = dbContext.DeploymentPathEnvironments.FirstOrDefault(x => x.DeploymentPath == deploymentPathId && x.Environment == targetEnvironmentId).StepNumber;
            if (stepNumber == 1)
            {
                return true;
            }

            int deploymentPathEnvironment = dbContext.DeploymentPathEnvironments.FirstOrDefault(x => x.DeploymentPath == deploymentPathId && x.StepNumber == stepNumber - 1).Environment;

            return dbContext.Actions.Any(x => x.Solution == solutionId && x.TargetEnvironment == deploymentPathEnvironment && x.Result == 1);
        }
        private bool ExportExists(int solutionId)
        {
            return base.dbContext.Actions.Any(a => a.Solution == solutionId && a.Type == 1 && a.Result == 1);
        }

        private bool SolutionExists(int key)
        {
            return base.dbContext.Solutions.Any(p => p.Id == key);
        }
    }
}
