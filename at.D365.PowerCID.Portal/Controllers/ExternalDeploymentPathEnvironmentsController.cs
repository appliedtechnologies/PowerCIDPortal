using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace at.D365.PowerCID.Portal.Controllers
{
    /// <summary>
    /// Manages the ordered steps (environments) of an <see cref="ExternalDeploymentPath"/>. Mirrors
    /// <see cref="DeploymentPathEnvironmentsController"/> for the external delivery area. Only available to the
    /// vendor tenant.
    /// </summary>
    [Authorize(Roles = "atPowerCID.Admin, atPowerCID.ExternalReleaseManager")]
    [CrossTenantDeliveryGate]
    public class ExternalDeploymentPathEnvironmentsController : BaseController
    {
        private readonly ILogger logger;

        public ExternalDeploymentPathEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ExternalDeploymentPathEnvironmentsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<ExternalDeploymentPathEnvironment> Get()
        {
            logger.LogDebug("Begin & End: ExternalDeploymentPathEnvironmentsController Get()");

            return base.dbContext.ExternalDeploymentPathEnvironments.OrderBy(s => s.StepNumber).Where(e => e.ExternalDeploymentPathNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyExternalDeploymentPath, [FromODataUri] int keyExternalEnvironment)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController Delete(keyExternalDeploymentPath: {keyExternalDeploymentPath}, keyExternalEnvironment: {keyExternalEnvironment})");

            if ((await this.dbContext.ExternalDeploymentPaths.FirstOrDefaultAsync(e => e.Id == keyExternalDeploymentPath && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var externalDeploymentPathEnvironmentToDelete = await this.dbContext.ExternalDeploymentPathEnvironments.FindAsync(keyExternalDeploymentPath, keyExternalEnvironment);

            if (externalDeploymentPathEnvironmentToDelete == null)
                return NotFound();

            var stepNumber = externalDeploymentPathEnvironmentToDelete.StepNumber;
            SortWhenDeleted(keyExternalDeploymentPath, stepNumber);
            this.dbContext.Remove(externalDeploymentPathEnvironmentToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController Delete(keyExternalDeploymentPath: {keyExternalDeploymentPath}, keyExternalEnvironment: {keyExternalEnvironment})");

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ExternalDeploymentPathEnvironment externalDeploymentPathEnvironment)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController Post(externalDeploymentPathEnvironment StepNumber: {externalDeploymentPathEnvironment.StepNumber})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if ((await this.dbContext.ExternalDeploymentPaths.FirstOrDefaultAsync(e => e.Id == externalDeploymentPathEnvironment.ExternalDeploymentPath && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            if ((await this.dbContext.ExternalEnvironments.FirstOrDefaultAsync(e => e.Id == externalDeploymentPathEnvironment.ExternalEnvironment)) == null)
                return NotFound();

            // All environments used by one external deployment path must belong to the same foreign tenant.
            System.Guid newStepTenantMsId = await this.dbContext.ExternalEnvironments
                .Where(e => e.Id == externalDeploymentPathEnvironment.ExternalEnvironment)
                .Select(e => e.EnvironmentNavigation.TenantNavigation.MsId)
                .FirstAsync();

            System.Guid existingStepTenantMsId = await this.dbContext.ExternalDeploymentPathEnvironments
                .Where(e => e.ExternalDeploymentPath == externalDeploymentPathEnvironment.ExternalDeploymentPath)
                .Select(e => e.ExternalEnvironmentNavigation.EnvironmentNavigation.TenantNavigation.MsId)
                .FirstOrDefaultAsync();

            if (existingStepTenantMsId != default && existingStepTenantMsId != newStepTenantMsId)
                return BadRequest(new ODataError { Code = "400", Message = "All environments of an external deployment path must belong to the same foreign (customer) tenant." });

            SortWhenAdded(externalDeploymentPathEnvironment.ExternalDeploymentPath, externalDeploymentPathEnvironment.StepNumber);
            base.dbContext.ExternalDeploymentPathEnvironments.Add(externalDeploymentPathEnvironment);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController Post(externalDeploymentPathEnvironment StepNumber: {externalDeploymentPathEnvironment.StepNumber})");

            return Created(externalDeploymentPathEnvironment);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyExternalEnvironment, [FromODataUri] int keyExternalDeploymentPath, [FromBody] object parameters)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController Patch(keyExternalEnvironment: {keyExternalEnvironment}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if ((await this.dbContext.ExternalDeploymentPaths.FirstOrDefaultAsync(e => e.Id == keyExternalDeploymentPath && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var parametersAsJObject = JsonConvert.DeserializeObject<JObject>(parameters.ToString());
            int fromIndex = (int)parametersAsJObject["FromIndex"];
            int toIndex = (int)parametersAsJObject["ToIndex"];

            if (toIndex > dbContext.ExternalDeploymentPathEnvironments.Count(e => e.ExternalDeploymentPath == keyExternalDeploymentPath))
                return BadRequest(new ODataError { Code = "400", Message = "ToIndex is out of range." });

            SortWhenUpdated(keyExternalDeploymentPath, fromIndex, toIndex);

            ExternalDeploymentPathEnvironment stepToUpdate = dbContext.ExternalDeploymentPathEnvironments.FirstOrDefault(x => x.ExternalEnvironment == keyExternalEnvironment && x.ExternalDeploymentPath == keyExternalDeploymentPath);
            stepToUpdate.StepNumber = toIndex;

            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController Patch(keyExternalEnvironment: {keyExternalEnvironment}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            return Updated(stepToUpdate);
        }

        private void SortWhenUpdated(int externalDeploymentPathId, int fromIndex, int toIndex)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController SortWhenUpdated(externalDeploymentPathId: {externalDeploymentPathId}, fromIndex: {fromIndex}, toIndex: {toIndex})");

            if (fromIndex < toIndex)
            {
                var stepsInBetween = dbContext.ExternalDeploymentPathEnvironments.Where(x => x.ExternalDeploymentPath == externalDeploymentPathId && x.StepNumber > fromIndex && x.StepNumber <= toIndex);

                foreach (var step in stepsInBetween)
                    step.StepNumber = step.StepNumber - 1;
            }
            else
            {
                var stepsInBetween = dbContext.ExternalDeploymentPathEnvironments.Where(x => x.ExternalDeploymentPath == externalDeploymentPathId && x.StepNumber < fromIndex && x.StepNumber >= toIndex);

                foreach (var step in stepsInBetween)
                    step.StepNumber = step.StepNumber + 1;
            }

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController SortWhenUpdated(externalDeploymentPathId: {externalDeploymentPathId}, fromIndex: {fromIndex}, toIndex: {toIndex})");
        }

        private void SortWhenAdded(int externalDeploymentPathId, int stepNumber)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController SortWhenAdded(externalDeploymentPathId: {externalDeploymentPathId}, stepNumber: {stepNumber})");

            var stepsWithHigherNumber = dbContext.ExternalDeploymentPathEnvironments.Where(e => e.StepNumber >= stepNumber && e.ExternalDeploymentPath == externalDeploymentPathId);
            foreach (var step in stepsWithHigherNumber)
                step.StepNumber = step.StepNumber + 1;

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController SortWhenAdded(externalDeploymentPathId: {externalDeploymentPathId}, stepNumber: {stepNumber})");
        }

        private void SortWhenDeleted(int externalDeploymentPathId, int stepNumber)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController SortWhenDeleted(externalDeploymentPathId: {externalDeploymentPathId}, stepNumber: {stepNumber})");

            var stepsWithHigherNumber = dbContext.ExternalDeploymentPathEnvironments.Where(e => e.StepNumber >= stepNumber && e.ExternalDeploymentPath == externalDeploymentPathId);
            foreach (var step in stepsWithHigherNumber)
                step.StepNumber = step.StepNumber - 1;

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController SortWhenDeleted(externalDeploymentPathId: {externalDeploymentPathId}, stepNumber: {stepNumber})");
        }
    }
}
