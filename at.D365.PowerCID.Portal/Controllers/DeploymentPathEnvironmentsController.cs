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
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.OData;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class DeploymentPathEnvironmentsController : BaseController
    {
        private readonly ILogger logger;
        public DeploymentPathEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<DeploymentPathEnvironmentsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<DeploymentPathEnvironment> Get()
        {
            logger.LogDebug($"Begin & End: DeploymentPathEnvironmentsController Get()");

            return base.dbContext.DeploymentPathEnvironments.OrderBy(s => s.StepNumber).Where(e => e.EnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromODataUri] int keyDeploymentPath, [FromODataUri] int keyEnvironment)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController Delete(keyDeploymentPath: {keyDeploymentPath}, keyEnvironment: {keyEnvironment})");

            if ((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == keyEnvironment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var deploymentPathEnvironmentToDelete = await this.dbContext.DeploymentPathEnvironments.FindAsync(keyDeploymentPath, keyEnvironment);

            if (deploymentPathEnvironmentToDelete == null)
                return NotFound();

            var stepnumber = deploymentPathEnvironmentToDelete.StepNumber;
            sortWhenDeleted(keyDeploymentPath, keyEnvironment, stepnumber);
            this.dbContext.Remove(deploymentPathEnvironmentToDelete);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: DeploymentPathEnvironmentsController Delete(keyDeploymentPath: {keyDeploymentPath}, keyEnvironment: {keyEnvironment})");

            return Ok();
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DeploymentPathEnvironment deploymentPathEnvironment)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController Post(deploymentPathEnvironment stepnumber: {deploymentPathEnvironment.StepNumber})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == deploymentPathEnvironment.Environment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var stepnumber = deploymentPathEnvironment.StepNumber;
            /* var maxStepNumberFind = base.dbContext.DeploymentPathEnvironments.Max(s => s.StepNumber);
            deploymentPathEnvironment.StepNumber = maxStepNumberFind + 1;
 */

            sortWhenAdded(deploymentPathEnvironment.DeploymentPath, deploymentPathEnvironment.Environment, deploymentPathEnvironment.StepNumber);
            base.dbContext.DeploymentPathEnvironments.Add(deploymentPathEnvironment);

            base.dbContext.DeploymentPathEnvironments.OrderBy(s => s.StepNumber);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: DeploymentPathEnvironmentsController Post(deploymentPathEnvironment stepnumber: {deploymentPathEnvironment.StepNumber})");

            return Created(deploymentPathEnvironment);
        }

        [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager")]
        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyEnvironment, [FromODataUri] int keyDeploymentPath, [FromBody] Object parameters)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController Patch(keyEnvironment: {keyEnvironment}, keyDeploymentPath: {keyDeploymentPath})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if ((await this.dbContext.Environments.FirstOrDefaultAsync(e => e.Id == keyEnvironment && e.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var parametersAsJObject = JsonConvert.DeserializeObject<JObject>(parameters.ToString());
            int fromIndex = (int)parametersAsJObject["FromIndex"];
            int toIndex = (int)parametersAsJObject["ToIndex"];

            if(toIndex > dbContext.DeploymentPathEnvironments.Count(e => e.DeploymentPath == keyDeploymentPath))
                return BadRequest(new ODataError { ErrorCode =  "400", Message = "ToIndex is out of range." });

            sortWhenUpdated(keyDeploymentPath, fromIndex, toIndex);

            //ApplicationDeploymentPath applicationDeploymentPathFromIndex = dbContext.ApplicationDeploymentPaths.FirstOrDefault(x => x.Application == keyApplication && x.DeploymentPath == keyDeploymentPath);
            //applicationDeploymentPathFromIndex.HierarchieNumber = toIndex;

            DeploymentPathEnvironment deploymentPathEnvironmenToUpdate = dbContext.DeploymentPathEnvironments.FirstOrDefault(x => x.Environment == keyEnvironment && x.DeploymentPath == keyDeploymentPath);
            deploymentPathEnvironmenToUpdate.StepNumber = toIndex;

            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: DeploymentPathEnvironmentsController Patch(keyEnvironment: {keyEnvironment}, keyDeploymentPath: {keyDeploymentPath})");

            return Updated(deploymentPathEnvironmenToUpdate);
        }

        private void sortWhenUpdated(int deploymentPathId, int fromIndex, int toIndex)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController sortWhenUpdated(deploymentPathId: {deploymentPathId}, fromIndex: {fromIndex}, toIndex:{toIndex})");

            if (fromIndex < toIndex)
            {
                var DeploymentPathEnvironmentsInBetween = dbContext.DeploymentPathEnvironments.Where(x => x.DeploymentPath == deploymentPathId && x.StepNumber > fromIndex && x.StepNumber <= toIndex);

                foreach (var depolymentPathEnvironment in DeploymentPathEnvironmentsInBetween)
                {
                    depolymentPathEnvironment.StepNumber = depolymentPathEnvironment.StepNumber - 1;
                }
                logger.LogDebug($"End: DeploymentPathEnvironmentsController sortWhenUpdated(deploymentPathId: {deploymentPathId}, fromIndex: {fromIndex}, toIndex:{toIndex})");
            }
            else
            {
                var DeploymentPathEnvironmentsInBetween = dbContext.DeploymentPathEnvironments.Where(x => x.DeploymentPath == deploymentPathId && x.StepNumber < fromIndex && x.StepNumber >= toIndex);

                foreach (var depolymentPathEnvironment in DeploymentPathEnvironmentsInBetween)
                {
                    depolymentPathEnvironment.StepNumber = depolymentPathEnvironment.StepNumber + 1;
                }
                logger.LogDebug($"End: DeploymentPathEnvironmentsController sortWhenUpdated(deploymentPathId: {deploymentPathId}, fromIndex: {fromIndex}, toIndex:{toIndex})");
            }
        }


        private void sortWhenAdded(int deploymentPathId, int enviromentId, int Stepnumber)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController sortWhenUpdated(deploymentPathId: {deploymentPathId}, enviromentId: {enviromentId}, Stepnumber:{Stepnumber})");

            var deploymentPathEnvironmentsWithHigherNumber = dbContext.DeploymentPathEnvironments.Where(e => e.StepNumber >= Stepnumber && e.DeploymentPath == deploymentPathId);
            foreach (var deploymentPathEnvironment in deploymentPathEnvironmentsWithHigherNumber)
            {
                deploymentPathEnvironment.StepNumber = deploymentPathEnvironment.StepNumber + 1;

            }
            logger.LogDebug($"End: DeploymentPathEnvironmentsController sortWhenUpdated(deploymentPathId: {deploymentPathId}, enviromentId: {enviromentId}, Stepnumber:{Stepnumber})");
        }

        private void sortWhenDeleted(int deploymentPathId, int enviromentId, int Stepnumber)
        {
            logger.LogDebug($"Begin: DeploymentPathEnvironmentsController sortWhenUpdated(deploymentPathId: {deploymentPathId}, enviromentId: {enviromentId}, Stepnumber:{Stepnumber})");

            var deploymentPathEnvironmentsWithHigherNumber = dbContext.DeploymentPathEnvironments.Where(e => e.StepNumber >= Stepnumber && e.DeploymentPath == deploymentPathId);
            foreach (var deploymentPathEnvironment in deploymentPathEnvironmentsWithHigherNumber)
            {
                deploymentPathEnvironment.StepNumber = deploymentPathEnvironment.StepNumber - 1;

            }
            logger.LogDebug($"End: DeploymentPathEnvironmentsController sortWhenUpdated(deploymentPathId: {deploymentPathId}, enviromentId: {enviromentId}, Stepnumber:{Stepnumber})");
        }
    }
}