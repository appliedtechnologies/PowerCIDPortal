using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using at.D365.PowerCID.Portal.Helpers;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class EnvironmentVariablesController : BaseController
    {
        private readonly ILogger logger;
        public EnvironmentVariablesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<EnvironmentVariablesController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        [EnableQuery]
        public IQueryable<EnvironmentVariable> Get()
        {
            logger.LogDebug($"Begin & End: EnvironmentVariablesController Get()");

            return base.dbContext.EnvironmentVariables.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] EnvironmentVariable environmentVariable)
        {
            logger.LogDebug($"Begin: EnvironmentVariablesController Post(environmentVariable MsId: {environmentVariable.MsId} )");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (dbContext.EnvironmentVariables.Any(x => x.MsId == environmentVariable.MsId && x.Application == environmentVariable.Application))
            {
                return BadRequest("Environment Variable already exists with this Ms Id and Application");
            }

            base.dbContext.EnvironmentVariables.Add(environmentVariable);
            await base.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: EnvironmentVariablesController Post(environmentVariable MsId: {environmentVariable.MsId} )");

            return Created(environmentVariable);
        }

        [HttpPost]
        public async Task<IEnumerable<EnvironmentVariable>> GetEnvironmentVariablesForApplication(ODataActionParameters parameters, [FromServices] EnvironmentVariableService environmentVariableService)
        {
            logger.LogDebug($"Begin: EnvironmentVariablesController GetEnvironmentVariablesForApplication(parameters applicationId: {(int)parameters["applicationId"]})");

            int applicationId = (int)parameters["applicationId"];
            var environmentVariables = await environmentVariableService.GetExistsingEnvironmentVariablesFromDataverse(applicationId);

            logger.LogDebug($"End: EnvironmentVariablesController GetEnvironmentVariablesForApplication(parameters applicationId: {(int)parameters["applicationId"]})");

            return environmentVariables;
        }
    }
}