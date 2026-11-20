using System;
using System.Collections.Generic;
using System.Data;
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
    /// <see cref="DeploymentPathEnvironmentsController"/> for the external deployment area. Only available to the
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

            return base.dbContext.ExternalDeploymentPathEnvironments
                .Where(e => e.ExternalDeploymentPathNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)
                .OrderBy(e => e.StepNumber)
                .ThenBy(e => e.ExternalEnvironment);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyExternalDeploymentPath, [FromODataUri] int keyExternalEnvironment)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController Delete(keyExternalDeploymentPath: {keyExternalDeploymentPath}, keyExternalEnvironment: {keyExternalEnvironment})");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(dbContext, $"ExternalDeploymentPathEnvironments:{keyExternalDeploymentPath}"))
                return Conflict("Another external environment order change is in progress. Retry the operation.");

            if (!await CanAccessPathAsync(keyExternalDeploymentPath))
                return Forbid();

            var externalDeploymentPathEnvironmentToDelete = await dbContext.ExternalDeploymentPathEnvironments.FirstOrDefaultAsync(e =>
                e.ExternalDeploymentPath == keyExternalDeploymentPath &&
                e.ExternalEnvironment == keyExternalEnvironment);
            if (externalDeploymentPathEnvironmentToDelete == null)
                return NotFound();

            var assignments = await GetOrderedAssignmentsAsync(keyExternalDeploymentPath);
            assignments.Remove(externalDeploymentPathEnvironmentToDelete);
            Renumber(assignments);
            if (!HasValidStepSequence(assignments))
                return Conflict("The external environment order could not be normalized.");

            dbContext.Remove(externalDeploymentPathEnvironmentToDelete);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController Delete(keyExternalDeploymentPath: {keyExternalDeploymentPath}, keyExternalEnvironment: {keyExternalEnvironment})");

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] object parameters)
        {
            if (!TryReadInsertRequest(parameters, out var externalDeploymentPathEnvironment, out var expectedOrder))
                return BadRequest("ExternalDeploymentPath, ExternalEnvironment, StepNumber, and ExpectedOrder are required.");

            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController Post(externalDeploymentPathEnvironment StepNumber: {externalDeploymentPathEnvironment.StepNumber})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(
                    dbContext,
                    $"ExternalDeploymentPathEnvironments:{externalDeploymentPathEnvironment.ExternalDeploymentPath}"))
                return Conflict("Another external environment order change is in progress. Retry the operation.");

            if (!await CanAccessPathAsync(externalDeploymentPathEnvironment.ExternalDeploymentPath))
                return Forbid();

            if (!await dbContext.ExternalEnvironments.AnyAsync(e => e.Id == externalDeploymentPathEnvironment.ExternalEnvironment))
                return NotFound();

            // All environments used by one external deployment path must belong to the same foreign tenant.
            Guid newStepTenantMsId = await dbContext.ExternalEnvironments
                .Where(e => e.Id == externalDeploymentPathEnvironment.ExternalEnvironment)
                .Select(e => e.EnvironmentNavigation.TenantNavigation.MsId)
                .FirstAsync();

            Guid existingStepTenantMsId = await dbContext.ExternalDeploymentPathEnvironments
                .Where(e => e.ExternalDeploymentPath == externalDeploymentPathEnvironment.ExternalDeploymentPath)
                .Select(e => e.ExternalEnvironmentNavigation.EnvironmentNavigation.TenantNavigation.MsId)
                .FirstOrDefaultAsync();

            if (existingStepTenantMsId != default && existingStepTenantMsId != newStepTenantMsId)
                return BadRequest(new ODataError { Code = "400", Message = "All environments of an external deployment path must belong to the same foreign (customer) tenant." });

            if (await dbContext.ExternalDeploymentPathEnvironments.AnyAsync(e =>
                    e.ExternalDeploymentPath == externalDeploymentPathEnvironment.ExternalDeploymentPath &&
                    e.ExternalEnvironment == externalDeploymentPathEnvironment.ExternalEnvironment))
                return BadRequest("No changes");

            var assignments = await GetOrderedAssignmentsAsync(externalDeploymentPathEnvironment.ExternalDeploymentPath);
            if (!assignments.Select(e => e.ExternalEnvironment).SequenceEqual(expectedOrder))
                return Conflict("The external environment order changed. Reload the deployment path and retry the insert.");

            var toIndex = externalDeploymentPathEnvironment.StepNumber;
            if (toIndex < 1 || toIndex > assignments.Count + 1)
                return BadRequest($"StepNumber must be between 1 and {assignments.Count + 1}.");

            assignments.Insert(toIndex - 1, externalDeploymentPathEnvironment);
            Renumber(assignments);
            if (!HasValidStepSequence(assignments))
                return Conflict("The external environment order could not be validated.");

            dbContext.ExternalDeploymentPathEnvironments.Add(externalDeploymentPathEnvironment);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController Post(externalDeploymentPathEnvironment StepNumber: {externalDeploymentPathEnvironment.StepNumber})");

            return Created(externalDeploymentPathEnvironment);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyExternalEnvironment, [FromODataUri] int keyExternalDeploymentPath, [FromBody] object parameters)
        {
            logger.LogDebug($"Begin: ExternalDeploymentPathEnvironmentsController Patch(keyExternalEnvironment: {keyExternalEnvironment}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryReadReorderRequest(parameters, out var fromIndex, out var toIndex, out var expectedOrder))
                return BadRequest("FromIndex, ToIndex, and ExpectedOrder are required.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(dbContext, $"ExternalDeploymentPathEnvironments:{keyExternalDeploymentPath}"))
                return Conflict("Another external environment order change is in progress. Retry the operation.");

            if (!await CanAccessPathAsync(keyExternalDeploymentPath))
                return Forbid();

            var assignments = await GetOrderedAssignmentsAsync(keyExternalDeploymentPath);
            var assignment = assignments.FirstOrDefault(e => e.ExternalEnvironment == keyExternalEnvironment);
            if (assignment == null)
                return NotFound();

            if (!assignments.Select(e => e.ExternalEnvironment).SequenceEqual(expectedOrder))
                return Conflict("The external environment order changed. Reload the deployment path and retry the move.");

            if (fromIndex < 1 || fromIndex > assignments.Count)
                return BadRequest($"FromIndex must be between 1 and {assignments.Count}.");
            if (toIndex < 1 || toIndex > assignments.Count)
                return BadRequest($"ToIndex must be between 1 and {assignments.Count}.");

            var currentIndex = assignments.IndexOf(assignment) + 1;
            if (fromIndex != currentIndex)
                return Conflict("The external environment order changed. Reload the deployment path and retry the move.");

            assignments.RemoveAt(currentIndex - 1);
            assignments.Insert(toIndex - 1, assignment);
            Renumber(assignments);
            if (!HasValidStepSequence(assignments))
                return Conflict("The external environment order could not be validated.");

            await base.dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ExternalDeploymentPathEnvironmentsController Patch(keyExternalEnvironment: {keyExternalEnvironment}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            return Updated(assignment);
        }

        private Task<bool> CanAccessPathAsync(int externalDeploymentPathId)
        {
            return dbContext.ExternalDeploymentPaths.AnyAsync(e =>
                e.Id == externalDeploymentPathId &&
                e.TenantNavigation.MsId == msIdTenantCurrentUser);
        }

        private async Task<List<ExternalDeploymentPathEnvironment>> GetOrderedAssignmentsAsync(int externalDeploymentPathId)
        {
            return await dbContext.ExternalDeploymentPathEnvironments
                .Where(e => e.ExternalDeploymentPath == externalDeploymentPathId)
                .OrderBy(e => e.StepNumber)
                .ThenBy(e => e.ExternalEnvironment)
                .ToListAsync();
        }

        private static void Renumber(IList<ExternalDeploymentPathEnvironment> assignments)
        {
            for (var index = 0; index < assignments.Count; index++)
                assignments[index].StepNumber = index + 1;
        }

        private static bool HasValidStepSequence(IList<ExternalDeploymentPathEnvironment> assignments)
        {
            return assignments.Select((assignment, index) => assignment.StepNumber == index + 1).All(isValid => isValid);
        }

        private static bool TryReadInsertRequest(
            object parameters,
            out ExternalDeploymentPathEnvironment assignment,
            out int[] expectedOrder)
        {
            assignment = null;
            expectedOrder = Array.Empty<int>();
            if (!TryReadRequestObject(parameters, out var request) ||
                !TryReadExpectedOrder(request["ExpectedOrder"], out expectedOrder))
                return false;

            try
            {
                assignment = request.ToObject<ExternalDeploymentPathEnvironment>();
                return assignment != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryReadReorderRequest(
            object parameters,
            out int fromIndex,
            out int toIndex,
            out int[] expectedOrder)
        {
            fromIndex = 0;
            toIndex = 0;
            expectedOrder = Array.Empty<int>();
            if (!TryReadRequestObject(parameters, out var request))
                return false;

            if (request["FromIndex"]?.Type != JTokenType.Integer ||
                request["ToIndex"]?.Type != JTokenType.Integer ||
                !int.TryParse(request["FromIndex"].ToString(), out fromIndex) ||
                !int.TryParse(request["ToIndex"].ToString(), out toIndex) ||
                !TryReadExpectedOrder(request["ExpectedOrder"], out expectedOrder))
                return false;

            return true;
        }

        private static bool TryReadRequestObject(object parameters, out JObject request)
        {
            request = null;
            if (parameters == null)
                return false;

            try
            {
                request = parameters as JObject ?? JObject.Parse(parameters.ToString());
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryReadExpectedOrder(JToken token, out int[] expectedOrder)
        {
            expectedOrder = Array.Empty<int>();
            if (token is not JArray orderTokens)
                return false;

            var parsedOrder = new List<int>(orderTokens.Count);
            foreach (var orderToken in orderTokens)
            {
                if (orderToken.Type != JTokenType.Integer || !int.TryParse(orderToken.ToString(), out var id))
                    return false;
                parsedOrder.Add(id);
            }

            expectedOrder = parsedOrder.ToArray();
            return true;
        }
    }
}
