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


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class ConnectionReferencesController : BaseController
    {
        private readonly ILogger logger;
        public ConnectionReferencesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ConnectionReferencesController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        } 

        [EnableQuery]
        public IQueryable<ConnectionReference> Get()
        {

            logger.LogDebug($"Begin: ConnectionReferencesController Get()");

            return base.dbContext.ConnectionReferences.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ConnectionReference connectionReference)
        {
            logger.LogDebug($"Begin: ConnectionReferencesController Post(connectionReference msid: {connectionReference.MsId})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (dbContext.ConnectionReferences.Any(x => x.MsId == connectionReference.MsId && x.Application == connectionReference.Application))
            {
                return BadRequest("Connection References already exists with this Ms Id and Application");
            }

            base.dbContext.ConnectionReferences.Add(connectionReference);
            await base.dbContext.SaveChangesAsync();

            return Created(connectionReference);
        }

        [HttpPost]
        public async Task<IEnumerable<ConnectionReference>> GetConnectionReferencesForApplication(ODataActionParameters parameters, [FromServices] ConnectionReferenceService connectionReferenceService)
        {

            logger.LogDebug($"Begin: ConnectionReferencesController GetConnectionReferencesForApplication(parameters: {(string)parameters["applicationId"]})");

            int applicationId = (int)parameters["applicationId"];
            var connectionReferences = await connectionReferenceService.GetExistsingConnectionReferencesFromDataverse(applicationId);

            return connectionReferences;
        }
    }
}