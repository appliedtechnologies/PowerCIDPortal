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
using Microsoft.OData;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class SolutionsController : BaseController
    {
        private readonly ILogger logger;

        public SolutionsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<SolutionsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<Solution> Get()
        {
            logger.LogDebug($"Begin & End: SolutionsController Get()");

            return base.dbContext.Solutions.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpGet]
        [Route("odata/Solutions({key})/DeploymentSettings")]
        public async Task<IActionResult> GetDeploymentSettings(
            [FromRoute] int key,
            [FromQuery] int environmentId,
            [FromServices] DeploymentSettingsService deploymentSettingsService)
        {
            var solution = await GetTenantSolution(key);
            if (solution == null)
                return Forbid();
            if (!CanAccessConfiguration(solution.Application, environmentId))
                return Forbid();

            var manifest = await deploymentSettingsService.GetManifest(key);
            if (manifest == null)
                return Ok(new { manifestStatus = DeploymentManifestStatus.Draft.ToString(), settings = Array.Empty<object>() });

            var settings = manifest.Settings
                .OrderBy(e => e.Kind)
                .ThenBy(e => e.MsId)
                .Select(setting => new
                {
                    setting.Id,
                    kind = setting.Kind.ToString(),
                    setting.MsId,
                    setting.LogicalName,
                    setting.DisplayName,
                    setting.ConnectorId,
                    setting.EnvironmentVariableType,
                    setting.DefaultValue,
                    setting.IsRequired,
                    value = setting.Values.FirstOrDefault(e => e.EnvironmentId == environmentId),
                    componentHash = setting.ComponentHash
                });

            return Ok(new
            {
                manifestStatus = manifest.Status.ToString(),
                manifestHash = manifest.ManifestHash,
                lastSyncedOn = manifest.LastSyncedOn,
                lastSyncError = manifest.LastSyncError,
                settings
            });
        }

        [HttpPost]
        public async Task<IActionResult> RefreshDeploymentSettings(
            [FromODataUri] int key,
            [FromServices] DeploymentSettingsService deploymentSettingsService)
        {
            if (await GetTenantSolution(key) == null)
                return Forbid();

            try
            {
                var manifest = await deploymentSettingsService.RefreshDeploymentManifest(key);
                return Ok(new
                {
                    manifestStatus = manifest.Status.ToString(),
                    manifestHash = manifest.ManifestHash,
                    settingCount = manifest.Settings.Count,
                    lastSyncedOn = manifest.LastSyncedOn
                });
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not refresh deployment settings for solution {SolutionId}.", key);
                return BadRequest(new ODataError
                {
                    Code = "DeploymentManifestSyncFailed",
                    Message = "The deployment settings manifest could not be refreshed."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDeploymentSettingsStatus(
            [FromODataUri] int key,
            ODataActionParameters parameters,
            [FromServices] DeploymentSettingsService deploymentSettingsService)
        {
            var solution = await GetTenantSolution(key);
            if (solution == null)
                return Forbid();

            var environmentId = (int)parameters["environmentId"];
            if (!CanAccessConfiguration(solution.Application, environmentId))
                return Forbid();

            return Ok(await deploymentSettingsService.GetStatus(key, environmentId));
        }

        [HttpPatch]
        [Route("odata/Solutions({solutionId})/DeploymentSettings/{settingId}")]
        public async Task<IActionResult> UpdateDeploymentSetting(
            [FromRoute] int solutionId,
            [FromRoute] int settingId,
            [FromBody] DeploymentSettingValueUpdateRequest request,
            [FromServices] DeploymentSettingsService deploymentSettingsService)
        {
            var solution = await GetTenantSolution(solutionId);
            if (solution == null)
                return Forbid();
            if (!CanAccessConfiguration(solution.Application, request.EnvironmentId))
                return Forbid();
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var rowVersion = string.IsNullOrWhiteSpace(request.RowVersion)
                    ? null
                    : Convert.FromBase64String(request.RowVersion);
                var value = await deploymentSettingsService.UpdateValue(
                    solutionId,
                    settingId,
                    request.EnvironmentId,
                    request.Value,
                    request.IsConfigured,
                    rowVersion);
                return Ok(new
                {
                    value.Id,
                    value.SettingId,
                    value.EnvironmentId,
                    value.IsConfigured,
                    value.IsInherited,
                    value.InheritedFromSolutionId,
                    rowVersion = value.RowVersion == null ? null : Convert.ToBase64String(value.RowVersion)
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new ODataError
                {
                    Code = "ConcurrencyConflict",
                    Message = "The deployment setting was changed by another user."
                });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new ODataError { Code = "InvalidDeploymentSetting", Message = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new ODataError { Code = "DeploymentSettingNotFound", Message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new ODataError { Code = "ConcurrencyConflict", Message = exception.Message });
            }
        }

        [HttpPost]
        [Route("odata/Solutions({solutionId})/DeploymentSettings/{settingId}/Reset")]
        public async Task<IActionResult> ResetDeploymentSetting(
            [FromRoute] int solutionId,
            [FromRoute] int settingId,
            [FromBody] DeploymentSettingResetRequest request,
            [FromServices] DeploymentSettingsService deploymentSettingsService)
        {
            var solution = await GetTenantSolution(solutionId);
            if (solution == null)
                return Forbid();

            var environmentId = request.EnvironmentId;
            if (!CanAccessConfiguration(solution.Application, environmentId))
                return Forbid();

            return Ok(await deploymentSettingsService.ResetValue(solutionId, settingId, environmentId));
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Solution> solution)
        {
            logger.LogDebug($"Begin: SolutionsController Patch(key: {key})");

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
            logger.LogDebug($"End: SolutionsController Patch(key: {key})");

            return Updated(entity);
        }

        [HttpPost]
        public async Task<IActionResult> GetSolutionAsBase64String([FromODataUri] int key, ODataActionParameters parameters, [FromServices] GitHubService gitHubService)
        {
            logger.LogDebug($"Begin: SolutionsController GetSolutionAsBase64String(key: {key}, unmanaged: {(bool)parameters["unmanaged"]})");

            if ((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            bool unmanaged = (bool)parameters["unmanaged"];

            Solution solution = dbContext.Solutions.FirstOrDefault(x => x.Id == key);
            Tenant tenant = dbContext.Solutions.FirstOrDefault(x => x.Id == key).ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation;

            var solutionAsBase64String = await gitHubService.GetSolutionFileAsBase64String(tenant, solution, unmanaged);

            logger.LogDebug($"End: SolutionsController GetSolutionAsBase64String(key: {key})");

            return Ok(solutionAsBase64String);
        }

        [HttpPost]
        public async Task<IActionResult> Export([FromODataUri] int key, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController Export(key: {key})");

            if ((await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            Data.Models.Action createdAction = await solutionService.AddExportAction(key, this.msIdTenantCurrentUser, this.msIdCurrentUser, exportOnly: true);

            logger.LogDebug($"End: SolutionsController Export(key: {key})");

            return Ok(createdAction);
        }

        [HttpPost]
        public async Task<IActionResult> Import([FromODataUri] int key, ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController Import(key: {key}, parameters targetEnvironmentId: {(int)parameters["targetEnvironmentId"]})");

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
                logger.LogDebug($"End: SolutionsController Import(key: {key}, parameters targetEnvironmentId: {(int)parameters["targetEnvironmentId"]})");

                return Ok(createdAction);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyUpgrade([FromODataUri] int key, ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController ApplyUpgrade(key: {key})");

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
            logger.LogDebug($"End: SolutionsController ApplyUpgrade()");

            return Ok(createdAction);
        }

        [HttpPost]
        public async Task<IActionResult> EnableFlows([FromODataUri] int key, ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController EnableFlows(key: {key})");

            var solution = await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
            if(solution == null)
                return Forbid();

            if(solution.IsPatch())
                return BadRequest("Can't enable flows for patch solution");

            int targetEnvironmentId = (int)parameters["targetEnvironmentId"];

            Data.Models.Action createdAction;
            try
            {
                createdAction = await solutionService.AddEnableFlowsAction(key, targetEnvironmentId, this.msIdCurrentUser);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            logger.LogDebug($"End: SolutionsController EnableFlows()");

            return Ok(createdAction);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.ExternalReleaseManager")]
        [CrossTenantDeliveryGate]
        [HttpPost]
        public async Task<IActionResult> SetExternalRelease([FromODataUri] int key, ODataActionParameters parameters)
        {
            logger.LogDebug($"Begin: SolutionsController SetExternalRelease(key: {key})");

            var solution = await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
            if (solution == null)
                return Forbid();

            bool released = (bool)parameters["released"];
            if (released && !ExportExists(key))
                return BadRequest("A solution version can only be released for external deployment after at least one successful export.");

            solution.IsReleasedExternally = released;
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: SolutionsController SetExternalRelease(key: {key})");

            return Ok(solution);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.ExternalDeployer")]
        [CrossTenantDeliveryGate]
        [HttpPost]
        public async Task<IActionResult> ExternalImport([FromODataUri] int key, ODataActionParameters parameters, [FromServices] SolutionService solutionService)
        {
            logger.LogDebug($"Begin: SolutionsController ExternalImport(key: {key})");

            var solution = await this.dbContext.Solutions.FirstOrDefaultAsync(e => e.Id == key && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
            if (solution == null)
                return Forbid();

            if (!ExportExists(key))
                return BadRequest("This solution version has not been exported yet. Export or import it internally at least once before delivering it externally.");

            int externalEnvironmentId = (int)parameters["externalEnvironmentId"];
            int externalDeploymentPathId = (int)parameters["externalDeploymentPathId"];

            Data.Models.Action createdAction;

            try
            {
                createdAction = await solutionService.AddExternalImportAction(key, externalEnvironmentId, externalDeploymentPathId, this.msIdCurrentUser);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            logger.LogDebug($"End: SolutionsController ExternalImport(key: {key})");

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

            logger.LogDebug($"End: SolutionsController IsPreviousDeploymentEnvironmentResultSuccessful(deploymentPathId: {deploymentPathId}, targetEnvironmentId: {targetEnvironmentId}, solutionId: {solutionId} )");

            return dbContext.Actions.Any(x => x.Solution == solutionId && x.TargetEnvironment == deploymentPathEnvironment && x.Result == 1);
        }
        private bool ExportExists(int solutionId)
        {
            logger.LogDebug($"Begin & End: SolutionsController ExportExists(solutionId: {solutionId})");

            return base.dbContext.Actions.Any(a => a.Solution == solutionId && a.Type == 1 && a.Result == 1);
        }

        private bool ImportExistsOnEnvironment(int solutionId, int environmentId)
        {
            logger.LogDebug($"Begin & End: SolutionsController ImportExistsOnEnvironment(solutionId: {solutionId}; environmentId: {environmentId})");

            return base.dbContext.Actions.Any(a => a.Solution == solutionId && a.TargetEnvironment == environmentId && a.Type == 2 && a.Result == 1);
        }

        private bool ApplyUpgradeExistsOnEnvironment(int solutionId, int environmentId)
        {
            logger.LogDebug($"Begin & End: SolutionsController ApplyUpgradeExistsOnEnvironment(solutionId: {solutionId}; environmentId: {environmentId})");

            return base.dbContext.Actions.Any(a => a.Solution == solutionId && a.TargetEnvironment == environmentId && a.Type == 3 && a.Result == 1);
        }

        private bool SolutionExists(int key)
        {
            logger.LogDebug($"Begin & End: SolutionsController SolutionExists(key: {key})");

            return base.dbContext.Solutions.Any(p => p.Id == key);
        }

        private Task<Solution> GetTenantSolution(int solutionId)
        {
            return this.dbContext.Solutions
                .Include(e => e.ApplicationNavigation)
                    .ThenInclude(e => e.DevelopmentEnvironmentNavigation)
                        .ThenInclude(e => e.TenantNavigation)
                .SingleOrDefaultAsync(e =>
                    e.Id == solutionId &&
                    e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }
    }

    public sealed class DeploymentSettingValueUpdateRequest
    {
        public int EnvironmentId { get; set; }
        public string Value { get; set; }
        public bool IsConfigured { get; set; }
        public string RowVersion { get; set; }
    }

    public sealed class DeploymentSettingResetRequest
    {
        public int EnvironmentId { get; set; }
    }
}
