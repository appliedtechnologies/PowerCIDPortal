using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using at.D365.PowerCID.Portal.Helpers;

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class ApplicationDeploymentPathsController : BaseController
    {
        private readonly ILogger logger;
        public ApplicationDeploymentPathsController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ApplicationDeploymentPathsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<ApplicationDeploymentPath> Get()
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Get()");

            return base.dbContext.ApplicationDeploymentPaths
                .Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)
                .OrderBy(ad => ad.HierarchieNumber == null)
                .ThenBy(ad => ad.HierarchieNumber)
                .ThenBy(ad => ad.DeploymentPath);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyApplication, [FromODataUri] int keyDeploymentPath)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Delete(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(dbContext, $"ApplicationDeploymentPaths:{keyApplication}"))
                return Conflict("Another deployment path order change is in progress. Retry the operation.");

            var applicationDeploymentPathToDelete = await dbContext.ApplicationDeploymentPaths.FirstOrDefaultAsync(e =>
                e.Application == keyApplication &&
                e.DeploymentPath == keyDeploymentPath &&
                e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == msIdTenantCurrentUser);
            if (applicationDeploymentPathToDelete == null)
                return NotFound();

            var assignments = await GetOrderedAssignments(keyApplication);
            assignments.Remove(applicationDeploymentPathToDelete);
            Renumber(assignments);
            if (!HasValidHierarchySequence(assignments))
                return Conflict("The deployment path order could not be normalized.");
            dbContext.Remove(applicationDeploymentPathToDelete);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ApplicationDeploymentPathsController Delete(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            return Ok();
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApplicationDeploymentPath applicationDeploymentPath)
        {
            if (applicationDeploymentPath == null)
                return BadRequest("Application and DeploymentPath are required.");

            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Post(applicationDeploymentPath: {applicationDeploymentPath.DeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!applicationDeploymentPath.HierarchieNumber.HasValue)
                return BadRequest("HierarchieNumber is required.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(
                    dbContext,
                    $"ApplicationDeploymentPaths:{applicationDeploymentPath.Application}"))
                return Conflict("Another deployment path order change is in progress. Retry the operation.");

            if (!await dbContext.Applications.AnyAsync(e =>
                    e.Id == applicationDeploymentPath.Application &&
                    e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            if (!await dbContext.DeploymentPaths.AnyAsync(e =>
                    e.Id == applicationDeploymentPath.DeploymentPath &&
                    e.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            if (await dbContext.ApplicationDeploymentPaths.AnyAsync(x =>
                    x.Application == applicationDeploymentPath.Application &&
                    x.DeploymentPath == applicationDeploymentPath.DeploymentPath))
                return BadRequest("No changes");

            var assignments = await GetOrderedAssignments(applicationDeploymentPath.Application);
            var toIndex = applicationDeploymentPath.HierarchieNumber.Value;
            if (toIndex < 1 || toIndex > assignments.Count + 1)
                return BadRequest($"HierarchieNumber must be between 1 and {assignments.Count + 1}.");

            assignments.Insert(toIndex - 1, applicationDeploymentPath);
            Renumber(assignments);
            if (!HasValidHierarchySequence(assignments))
                return Conflict("The deployment path order could not be validated.");
            dbContext.ApplicationDeploymentPaths.Add(applicationDeploymentPath);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ApplicationDeploymentPathsController Post(applicationDeploymentPath: {applicationDeploymentPath.DeploymentPath})");
            return Created(applicationDeploymentPath);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyApplication, [FromODataUri] int keyDeploymentPath, [FromBody] Object parameters)
        {
            logger.LogDebug($"Begin: ApplicationDeploymentPathsController Patch(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryReadReorderRequest(parameters, out var fromIndex, out var toIndex, out var expectedOrder))
                return BadRequest("FromIndex, ToIndex, and ExpectedOrder are required.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (!await DeploymentPathOrderLock.AcquireAsync(dbContext, $"ApplicationDeploymentPaths:{keyApplication}"))
                return Conflict("Another deployment path order change is in progress. Retry the operation.");

            if (!await dbContext.Applications.AnyAsync(e =>
                    e.Id == keyApplication &&
                    e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == msIdTenantCurrentUser))
                return Forbid();

            var assignments = await GetOrderedAssignments(keyApplication);
            var assignment = assignments.FirstOrDefault(x => x.DeploymentPath == keyDeploymentPath);
            if (assignment == null)
                return NotFound();

            if (!assignments.Select(x => x.DeploymentPath).SequenceEqual(expectedOrder))
                return Conflict("The deployment path order changed. Reload the application and retry the move.");

            if (fromIndex < 1 || fromIndex > assignments.Count)
                return BadRequest($"FromIndex must be between 1 and {assignments.Count}.");
            if (toIndex < 1 || toIndex > assignments.Count)
                return BadRequest($"ToIndex must be between 1 and {assignments.Count}.");

            var currentIndex = assignments.IndexOf(assignment) + 1;
            if (fromIndex != currentIndex)
                return Conflict("The deployment path order changed. Reload the application and retry the move.");

            assignments.RemoveAt(currentIndex - 1);
            assignments.Insert(toIndex - 1, assignment);
            Renumber(assignments);
            if (!HasValidHierarchySequence(assignments))
                return Conflict("The deployment path order could not be validated.");
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogDebug($"End: ApplicationDeploymentPathsController Patch(keyApplication: {keyApplication}, keyDeploymentPath: {keyDeploymentPath})");

            return Updated(assignment);
        }

        private async Task<List<ApplicationDeploymentPath>> GetOrderedAssignments(int applicationId)
        {
            return await dbContext.ApplicationDeploymentPaths
                .Where(x => x.Application == applicationId)
                .OrderBy(x => x.HierarchieNumber == null)
                .ThenBy(x => x.HierarchieNumber)
                .ThenBy(x => x.DeploymentPath)
                .ToListAsync();
        }

        private static void Renumber(IList<ApplicationDeploymentPath> assignments)
        {
            for (var index = 0; index < assignments.Count; index++)
                assignments[index].HierarchieNumber = index + 1;
        }

        private static bool HasValidHierarchySequence(IList<ApplicationDeploymentPath> assignments)
        {
            return assignments.Select((assignment, index) => assignment.HierarchieNumber == index + 1).All(isValid => isValid);
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

            if (request["FromIndex"]?.Type != JTokenType.Integer ||
                request["ToIndex"]?.Type != JTokenType.Integer ||
                !int.TryParse(request["FromIndex"].ToString(), out fromIndex) ||
                !int.TryParse(request["ToIndex"].ToString(), out toIndex) ||
                request["ExpectedOrder"] is not JArray orderTokens)
                return false;

            var parsedOrder = new List<int>(orderTokens.Count);
            foreach (var token in orderTokens)
            {
                if (token.Type != JTokenType.Integer || !int.TryParse(token.ToString(), out var id))
                    return false;
                parsedOrder.Add(id);
            }

            expectedOrder = parsedOrder.ToArray();
            return true;
        }
    }
}
