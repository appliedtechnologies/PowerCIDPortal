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

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class EnvironmentVariablesController : BaseController
    {
        public EnvironmentVariablesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        } 

        [EnableQuery]
        public IQueryable<EnvironmentVariable> Get()
        {
            return base.dbContext.EnvironmentVariables.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] EnvironmentVariable environmentVariable)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (dbContext.EnvironmentVariables.Any(x => x.MsId == environmentVariable.MsId))
            {
                return BadRequest("Environment Variable already exists with this Ms Id");
            }

            base.dbContext.EnvironmentVariables.Add(environmentVariable);
            await base.dbContext.SaveChangesAsync();

            return Created(environmentVariable);
        }

        [HttpPost]
        public async Task<IEnumerable<EnvironmentVariable>> GetEnvironmentVariablesForApplication(ODataActionParameters parameters, [FromServices] EnvironmentVariableService environmentVariableService)
        {
            int applicationId = (int)parameters["applicationId"];
            var environmentVariables = await environmentVariableService.GetExistsingEnvironmentVariablesFromDataverse(applicationId);

            return environmentVariables;
        }
    }
}