using System;
using System.Collections.Generic;
using System.Data;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using Newtonsoft.Json;

namespace at.D365.PowerCID.Portal.Controllers
{
    /// <summary>
    /// Maps an <see cref="Application"/> to the <see cref="ExternalDeploymentPath"/>(s) it can be delivered through.
    /// Mirrors <see cref="ApplicationDeploymentPathsController"/> for the external deployment area. Only available to
    /// the vendor tenant.
    /// </summary>
    [Authorize(Roles = "atPowerCID.Admin, atPowerCID.ExternalReleaseManager")]
    [CrossTenantDeliveryGate]
    public class ApplicationExternalDeploymentPathsController : BaseController
    {
        private readonly ILogger logger;

        public ApplicationExternalDeploymentPathsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ApplicationExternalDeploymentPathsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<ApplicationExternalDeploymentPath> Get()
        {
            logger.LogDebug("Begin: ApplicationExternalDeploymentPathsController Get()");

            return base.dbContext.ApplicationExternalDeploymentPaths
                .Where(e => e.ExternalDeploymentPathNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)
                .OrderBy(e => e.HierarchieNumber == null)
                .ThenBy(e => e.HierarchieNumber)
                .ThenBy(e => e.ExternalDeploymentPath);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyApplication, [FromODataUri] int keyExternalDeploymentPath)
        {
            logger.LogDebug($"Begin: ApplicationExternalDeploymentPathsController Delete(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(dbContext, $"ApplicationExternalDeploymentPaths:{keyApplication}"))
                return Conflict("Another external deployment path order change is in progress. Retry the operation.");

            if (!await dbContext.Applications.AnyAsync(e =>
                    e.Id == keyApplication &&
                    e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            var applicationExternalDeploymentPathToDelete = await dbContext.ApplicationExternalDeploymentPaths.FirstOrDefaultAsync(e =>
                e.Application == keyApplication &&
                e.ExternalDeploymentPath == keyExternalDeploymentPath &&
                e.ExternalDeploymentPathNavigation.TenantNavigation.MsId == msIdTenantCurrentUser);
            if (applicationExternalDeploymentPathToDelete == null)
                return NotFound();

            var assignments = await GetOrderedAssignmentsAsync(keyApplication);
            assignments.Remove(applicationExternalDeploymentPathToDelete);
            Renumber(assignments);
            if (!HasValidHierarchySequence(assignments))
                return Conflict("The external deployment path order could not be normalized.");

            dbContext.Remove(applicationExternalDeploymentPathToDelete);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ApplicationExternalDeploymentPathsController Delete(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] object parameters)
        {
            if (!TryReadInsertRequest(parameters, out var applicationExternalDeploymentPath, out var expectedOrder))
                return BadRequest("Application, ExternalDeploymentPath, and HierarchieNumber are required.");

            logger.LogDebug($"Begin: ApplicationExternalDeploymentPathsController Post(applicationExternalDeploymentPath: {applicationExternalDeploymentPath.ExternalDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!applicationExternalDeploymentPath.HierarchieNumber.HasValue)
                return BadRequest("HierarchieNumber is required.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(
                    dbContext,
                    $"ApplicationExternalDeploymentPaths:{applicationExternalDeploymentPath.Application}"))
                return Conflict("Another external deployment path order change is in progress. Retry the operation.");

            if (!await dbContext.Applications.AnyAsync(e =>
                    e.Id == applicationExternalDeploymentPath.Application &&
                    e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            if (!await dbContext.ExternalDeploymentPaths.AnyAsync(e =>
                    e.Id == applicationExternalDeploymentPath.ExternalDeploymentPath &&
                    e.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            if (await dbContext.ApplicationExternalDeploymentPaths.AnyAsync(x =>
                    x.Application == applicationExternalDeploymentPath.Application &&
                    x.ExternalDeploymentPath == applicationExternalDeploymentPath.ExternalDeploymentPath))
                return BadRequest("No changes");

            var assignments = await GetOrderedAssignmentsAsync(applicationExternalDeploymentPath.Application);
            if (expectedOrder != null &&
                !assignments.Select(x => x.ExternalDeploymentPath).SequenceEqual(expectedOrder))
                return Conflict("The external deployment path order changed. Reload the application and retry the insert.");

            var toIndex = applicationExternalDeploymentPath.HierarchieNumber.Value;
            if (toIndex < 1 || toIndex > assignments.Count + 1)
                return BadRequest($"HierarchieNumber must be between 1 and {assignments.Count + 1}.");

            assignments.Insert(toIndex - 1, applicationExternalDeploymentPath);
            Renumber(assignments);
            if (!HasValidHierarchySequence(assignments))
                return Conflict("The external deployment path order could not be validated.");

            dbContext.ApplicationExternalDeploymentPaths.Add(applicationExternalDeploymentPath);
            await base.dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ApplicationExternalDeploymentPathsController Post(applicationExternalDeploymentPath: {applicationExternalDeploymentPath.ExternalDeploymentPath})");

            return Created(applicationExternalDeploymentPath);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyApplication, [FromODataUri] int keyExternalDeploymentPath, [FromBody] object parameters)
        {
            logger.LogDebug($"Begin: ApplicationExternalDeploymentPathsController Patch(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryReadReorderRequest(parameters, out var fromIndex, out var toIndex, out var expectedOrder))
                return BadRequest("FromIndex and ToIndex are required.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(dbContext, $"ApplicationExternalDeploymentPaths:{keyApplication}"))
                return Conflict("Another external deployment path order change is in progress. Retry the operation.");

            if (!await dbContext.Applications.AnyAsync(e =>
                    e.Id == keyApplication &&
                    e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            if (!await dbContext.ExternalDeploymentPaths.AnyAsync(e =>
                    e.Id == keyExternalDeploymentPath &&
                    e.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            var assignments = await GetOrderedAssignmentsAsync(keyApplication);
            var assignment = assignments.FirstOrDefault(x => x.ExternalDeploymentPath == keyExternalDeploymentPath);
            if (assignment == null)
                return NotFound();

            if (expectedOrder != null &&
                !assignments.Select(x => x.ExternalDeploymentPath).SequenceEqual(expectedOrder))
                return Conflict("The external deployment path order changed. Reload the application and retry the move.");

            if (fromIndex < 1 || fromIndex > assignments.Count)
                return BadRequest($"FromIndex must be between 1 and {assignments.Count}.");
            if (toIndex < 1 || toIndex > assignments.Count)
                return BadRequest($"ToIndex must be between 1 and {assignments.Count}.");

            var currentIndex = assignments.IndexOf(assignment) + 1;
            if (fromIndex != currentIndex)
                return Conflict("The external deployment path order changed. Reload the application and retry the move.");

            assignments.RemoveAt(currentIndex - 1);
            assignments.Insert(toIndex - 1, assignment);
            Renumber(assignments);
            if (!HasValidHierarchySequence(assignments))
                return Conflict("The external deployment path order could not be validated.");

            await base.dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ApplicationExternalDeploymentPathsController Patch(keyApplication: {keyApplication}, keyExternalDeploymentPath: {keyExternalDeploymentPath})");

            return Updated(assignment);
        }

        private async Task<List<ApplicationExternalDeploymentPath>> GetOrderedAssignmentsAsync(int applicationId)
        {
            return await dbContext.ApplicationExternalDeploymentPaths
                .Where(x => x.Application == applicationId)
                .OrderBy(x => x.HierarchieNumber == null)
                .ThenBy(x => x.HierarchieNumber)
                .ThenBy(x => x.ExternalDeploymentPath)
                .ToListAsync();
        }

        private static void Renumber(IList<ApplicationExternalDeploymentPath> assignments)
        {
            for (var index = 0; index < assignments.Count; index++)
                assignments[index].HierarchieNumber = index + 1;
        }

        private static bool HasValidHierarchySequence(IList<ApplicationExternalDeploymentPath> assignments)
        {
            return assignments.Select((assignment, index) => assignment.HierarchieNumber == index + 1).All(isValid => isValid);
        }

        private static bool TryReadInsertRequest(
            object parameters,
            out ApplicationExternalDeploymentPath assignment,
            out int[] expectedOrder)
        {
            assignment = null;
            expectedOrder = null;
            if (!TryReadRequestObject(parameters, out var request) ||
                !TryReadInteger(request, "Application", out var applicationId) ||
                !TryReadInteger(request, "ExternalDeploymentPath", out var externalDeploymentPathId) ||
                !TryReadInteger(request, "HierarchieNumber", out var hierarchieNumber) ||
                !TryReadExpectedOrder(request["ExpectedOrder"], out expectedOrder))
                return false;

            assignment = new ApplicationExternalDeploymentPath
            {
                Application = applicationId,
                ExternalDeploymentPath = externalDeploymentPathId,
                HierarchieNumber = hierarchieNumber,
            };
            return true;
        }

        private static bool TryReadReorderRequest(
            object parameters,
            out int fromIndex,
            out int toIndex,
            out int[] expectedOrder)
        {
            fromIndex = 0;
            toIndex = 0;
            expectedOrder = null;
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
            expectedOrder = null;
            if (token == null || token.Type == JTokenType.Null)
                return true;

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

        private static bool TryReadInteger(JObject request, string propertyName, out int value)
        {
            value = 0;
            var token = request[propertyName];
            return token?.Type == JTokenType.Integer && int.TryParse(token.ToString(), out value);
        }
    }
}
