using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using at.D365.PowerCID.Portal.Helpers;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class EnvironmentVariableEnvironmentsController : BaseController
    {
        private readonly ILogger logger;

        public EnvironmentVariableEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<EnvironmentVariableEnvironmentsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<EnvironmentVariableEnvironment> Get()
        {
            logger.LogDebug($"Begin & End: EnvironmentVariableEnvironmentsController Get()");

            return base.dbContext.EnvironmentVariableEnvironments.Where(e => e.EnvironmentVariableNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] EnvironmentVariableEnvironment environmentVariableEnvironment)
        {
            logger.LogDebug($"Begin: EnvironmentVariableEnvironmentsController Post(environmentVariableEnvironment Environment: {environmentVariableEnvironment.Environment})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (dbContext.EnvironmentVariableEnvironments.Any(x => x.EnvironmentVariable == environmentVariableEnvironment.EnvironmentVariable && x.Environment == environmentVariableEnvironment.Environment))
            {
                return BadRequest("Environment Variable for this Environment already exists");
            }

            base.dbContext.EnvironmentVariableEnvironments.Add(environmentVariableEnvironment);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: EnvironmentVariableEnvironmentsController Post(environmentVariableEnvironment Environment: {environmentVariableEnvironment.Environment})");

            return Created(environmentVariableEnvironment);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyEnvironmentVariable, [FromODataUri] int keyEnvironment, Delta<EnvironmentVariableEnvironment> environmentVariableEnvironment)
        {
            logger.LogDebug($"Begin: EnvironmentVariableEnvironmentsController Patch(keyEnvironmentVariable: {keyEnvironmentVariable}, keyEnvironment: {keyEnvironment})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if ((await this.dbContext.EnvironmentVariables.FirstOrDefaultAsync(e => e.Id == keyEnvironmentVariable && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var entity = await base.dbContext.EnvironmentVariableEnvironments.FirstAsync(e => e.EnvironmentVariable == keyEnvironmentVariable && e.Environment == keyEnvironment);
            if (entity == null)
            {
                return NotFound();
            }
            environmentVariableEnvironment.Patch(entity);
            try
            {
                //remove EnvironmentVariableEnvironment without value
                if(String.IsNullOrWhiteSpace(entity.Value))
                    base.dbContext.Remove(entity);

                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EnvironmentVariableEnvironmentExists(keyEnvironmentVariable, keyEnvironment))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            logger.LogDebug($"End: EnvironmentVariableEnvironmentsController Patch(keyEnvironmentVariable: {keyEnvironmentVariable}, keyEnvironment: {keyEnvironment})");

            return Updated(entity);
        }

        private bool EnvironmentVariableEnvironmentExists(int keyEnvironmentVariableEnvironment, int keyEnvironment)
        {
            logger.LogDebug($"Begin & End: EnvironmentVariableEnvironmentsController EnvironmentVariableEnvironmentExists(keyEnvironmentVariableEnvironment: {keyEnvironmentVariableEnvironment}, keyEnvironment: {keyEnvironment})");

            return base.dbContext.EnvironmentVariableEnvironments.Any(p => p.EnvironmentVariable == keyEnvironmentVariableEnvironment && p.Environment == keyEnvironment);
        }
    }
}