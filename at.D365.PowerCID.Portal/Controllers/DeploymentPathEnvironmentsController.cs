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
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class DeploymentPathEnvironmentsController : BaseController
    {
        private readonly ILogger logger;
        public DeploymentPathEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<DeploymentPathEnvironmentsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<DeploymentPathEnvironment> Get()
        {
            logger.LogDebug($"Begin & End: DeploymentPathEnvironmentsController Get()");

            return base.dbContext.DeploymentPathEnvironments
                .Where(e => e.EnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)
                .OrderBy(e => e.StepNumber)
                .ThenBy(e => e.Environment);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyDeploymentPath, [FromODataUri] int keyEnvironment)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController Delete(keyDeploymentPath: {keyDeploymentPath}, keyEnvironment: {keyEnvironment})");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (!await CanAccessAssignmentAsync(keyDeploymentPath, keyEnvironment))
                return Forbid();

            var deploymentPathEnvironmentToDelete = await dbContext.DeploymentPathEnvironments.FirstOrDefaultAsync(e =>
                e.DeploymentPath == keyDeploymentPath && e.Environment == keyEnvironment);
            if (deploymentPathEnvironmentToDelete == null)
                return NotFound();

            var assignments = await GetOrderedAssignmentsAsync(keyDeploymentPath);
            assignments.Remove(deploymentPathEnvironmentToDelete);
            Renumber(assignments);
            if (!HasValidStepSequence(assignments))
                return Conflict("The environment order could not be normalized.");

            dbContext.Remove(deploymentPathEnvironmentToDelete);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: DeploymentPathEnvironmentsController Delete(keyDeploymentPath: {keyDeploymentPath}, keyEnvironment: {keyEnvironment})");

            return Ok();
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DeploymentPathEnvironment deploymentPathEnvironment)
        {
            if (deploymentPathEnvironment == null)
                return BadRequest("DeploymentPath and Environment are required.");

            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController Post(deploymentPathEnvironment stepnumber: {deploymentPathEnvironment.StepNumber})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (!await CanAccessAssignmentAsync(deploymentPathEnvironment.DeploymentPath, deploymentPathEnvironment.Environment))
                return Forbid();

            if (await dbContext.DeploymentPathEnvironments.AnyAsync(e =>
                    e.DeploymentPath == deploymentPathEnvironment.DeploymentPath &&
                    e.Environment == deploymentPathEnvironment.Environment))
                return BadRequest("No changes");

            var assignments = await GetOrderedAssignmentsAsync(deploymentPathEnvironment.DeploymentPath);
            var toIndex = deploymentPathEnvironment.StepNumber;
            if (toIndex < 1 || toIndex > assignments.Count + 1)
                return BadRequest($"StepNumber must be between 1 and {assignments.Count + 1}.");

            assignments.Insert(toIndex - 1, deploymentPathEnvironment);
            Renumber(assignments);
            if (!HasValidStepSequence(assignments))
                return Conflict("The environment order could not be validated.");

            dbContext.DeploymentPathEnvironments.Add(deploymentPathEnvironment);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: DeploymentPathEnvironmentsController Post(deploymentPathEnvironment stepnumber: {deploymentPathEnvironment.StepNumber})");

            return Created(deploymentPathEnvironment);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyEnvironment, [FromODataUri] int keyDeploymentPath, [FromBody] Object parameters)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController Patch(keyEnvironment: {keyEnvironment}, keyDeploymentPath: {keyDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryReadIndices(parameters, out var fromIndex, out var toIndex))
                return BadRequest("FromIndex and ToIndex must be integers.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            if (!await CanAccessAssignmentAsync(keyDeploymentPath, keyEnvironment))
                return Forbid();

            var assignments = await GetOrderedAssignmentsAsync(keyDeploymentPath);
            var assignment = assignments.FirstOrDefault(e => e.Environment == keyEnvironment);
            if (assignment == null)
                return NotFound();

            if (fromIndex < 1 || fromIndex > assignments.Count)
                return BadRequest($"FromIndex must be between 1 and {assignments.Count}.");
            if (toIndex < 1 || toIndex > assignments.Count)
                return BadRequest($"ToIndex must be between 1 and {assignments.Count}.");

            var currentIndex = assignments.IndexOf(assignment) + 1;
            if (fromIndex != currentIndex)
                return Conflict("The environment order changed. Reload the deployment path and retry the move.");

            assignments.RemoveAt(currentIndex - 1);
            assignments.Insert(toIndex - 1, assignment);
            Renumber(assignments);
            if (!HasValidStepSequence(assignments))
                return Conflict("The environment order could not be validated.");

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: DeploymentPathEnvironmentsController Patch(keyEnvironment: {keyEnvironment}, keyDeploymentPath: {keyDeploymentPath})");

            return Updated(assignment);
        }

        private async Task<bool> CanAccessAssignmentAsync(int deploymentPathId, int environmentId)
        {
            return await dbContext.DeploymentPaths.AnyAsync(path =>
                    path.Id == deploymentPathId &&
                    path.TenantNavigation.MsId == msIdTenantCurrentUser) &&
                await dbContext.Environments.AnyAsync(environment =>
                    environment.Id == environmentId &&
                    environment.TenantNavigation.MsId == msIdTenantCurrentUser);
        }

        private async Task<List<DeploymentPathEnvironment>> GetOrderedAssignmentsAsync(int deploymentPathId)
        {
            return await dbContext.DeploymentPathEnvironments
                .Where(e => e.DeploymentPath == deploymentPathId)
                .OrderBy(e => e.StepNumber)
                .ThenBy(e => e.Environment)
                .ToListAsync();
        }

        private static void Renumber(IList<DeploymentPathEnvironment> assignments)
        {
            for (var index = 0; index < assignments.Count; index++)
                assignments[index].StepNumber = index + 1;
        }

        private static bool HasValidStepSequence(IList<DeploymentPathEnvironment> assignments)
        {
            return assignments.Select((assignment, index) => assignment.StepNumber == index + 1).All(isValid => isValid);
        }

        private static bool TryReadIndices(object parameters, out int fromIndex, out int toIndex)
        {
            fromIndex = 0;
            toIndex = 0;
            if (parameters == null)
                return false;

            JObject request;
            try
            {
                request = parameters as JObject ?? JObject.Parse(parameters.ToString());
            }
            catch (JsonException)
            {
                return false;
            }

            return request["FromIndex"]?.Type == JTokenType.Integer &&
                request["ToIndex"]?.Type == JTokenType.Integer &&
                int.TryParse(request["FromIndex"].ToString(), out fromIndex) &&
                int.TryParse(request["ToIndex"].ToString(), out toIndex);
        }
    }
}