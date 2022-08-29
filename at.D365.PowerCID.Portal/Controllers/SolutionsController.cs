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
using Microsoft.Extensions.Logging;
using at.D365.PowerCID.Portal.Helpers;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class SolutionsController : BaseController
    {
        private readonly ILogger logger;

        public SolutionsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<SolutionsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<Solution> Get()
        {
            logger.LogDebug($"Begin: SolutionsController Get()");

            return base.dbContext.Solutions.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Solution> solution)
        {
            logger.LogDebug($"Begin: SolutionsController Patch(key: {key}, solution Patch exists: {solution.HasMethod("Patch")})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
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
        public async Task<IActionResult> GetSolutionAsBase64String([FromODataUri] int key, [FromServices] GitHubService gitHubService)
        {
            logger.LogDebug($"Begin: SolutionsController GetSolutionAsBase64String(key; {key}, gitHubService GetSolutionFileAsBase64String exists: {gitHubService.HasMethod("GetSolutionFileAsBase64String")})");

            if ((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            Solution solution = dbContext.Solutions.FirstOrDefault(x => x.Id == key);
            Tenant tenant = dbContext.Solutions.FirstOrDefault(x => x.Id == key).ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation;

            var solutionAsBase64String = await gitHubService.GetSolutionFileAsBase64String(tenant, solution);
            return Ok(solutionAsBase64String);
        }

        [HttpPost]
        public async Task<IActionResult> Export([FromODataUri] int key, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController Export(key: {key}, solutionService AddExportAction exists: {solutionService.HasMethod("AddExportAction")})");

            if ((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            Data.Models.Action createdAction = await solutionService.AddExportAction(key, this.msIdTenantCurrentUser, this.msIdCurrentUser, exportOnly: true);
            return Ok(createdAction);
        }

        [HttpPost]
        public async Task<IActionResult> Import([FromODataUri] int key, ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController Import(key: {key}, parameters targetEnvironmentId: {(int)parameters["targetEnvironmentId"]}, solutionService AddImportAction exists: {solutionService.HasMethod("AddImportAction")})");

            if ((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            int targetEnvironmentId = (int)parameters["targetEnvironmentId"];
            int deploymentPathId = (int)parameters["deploymentPathId"];

            int userId = dbContext.Users.FirstOrDefault(u => u.MsId == this.msIdCurrentUser).Id;

            if (IsPreviousDeploymentEnvironmentResultSuccessful(deploymentPathId, targetEnvironmentId, key) == false)
            {
                return BadRequest("Can't skip a previous deploymentenvironment");
            }

            else
            {
                Data.Models.Action createdAction;

                try
                {
                    if (ExportExists(key))
                        createdAction = await solutionService.AddImportAction(key, targetEnvironmentId, this.msIdCurrentUser);
                    else
                        createdAction = await solutionService.AddExportAction(key, this.msIdTenantCurrentUser, this.msIdCurrentUser, exportOnly: false, targetEnvironmentId);
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }

                return Ok(createdAction);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyUpgrade([FromODataUri] int key, ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController ApplyUpgrade(key: {key}, parameters targetEnvironmentId: {(int)parameters["targetEnvironmentId"]}, solutionService AddApplyUpgradeAction exists: {solutionService.HasMethod("AddApplyUpgradeAction")} )");

            var solution = await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
            if (solution == null)
                return Forbid();

            if (solution.IsPatch())
                return BadRequest("Can't apply upgrade for patch solution");

            int targetEnvironmentId = (int)parameters["targetEnvironmentId"];
            if (ImportExistsOnEnvironment(key, targetEnvironmentId) == false)
                return BadRequest("Can't skip import before applying an upgrade");

            Data.Models.Action createdAction;

            try
            {
                createdAction = await solutionService.AddApplyUpgradeAction(key, targetEnvironmentId, this.msIdCurrentUser);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok(createdAction);
        }

        private bool IsPreviousDeploymentEnvironmentResultSuccessful(int deploymentPathId, int targetEnvironmentId, int solutionId)
        {
            logger.LogDebug($"Begin: SolutionsController IsPreviousDeploymentEnvironmentResultSuccessful(deploymentPathId: {deploymentPathId}, targetEnvironmentId: {targetEnvironmentId}, solutionId: {solutionId} )");

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
            logger.LogDebug($"Begin: SolutionsController ExportExists(solutionId: {solutionId})");

            return base.dbContext.Actions.Any(a => a.Solution == solutionId && a.Type == 1 && a.Result == 1);
        }

        private bool ImportExistsOnEnvironment(int solutionId, int environmentId)
        {
            logger.LogDebug($"Begin: SolutionsController ImportExistsOnEnvironment(solutionId: {solutionId}; environmentId. {environmentId})");

            return base.dbContext.Actions.Any(a => a.Solution == solutionId && a.TargetEnvironment == environmentId && a.Type == 2 && a.Result == 1);
        }

        private bool SolutionExists(int key)
        {
            logger.LogDebug($"Begin: SolutionsController SolutionExists(key: {key})");

            return base.dbContext.Solutions.Any(p => p.Id == key);
        }
    }
}
