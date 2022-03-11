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
    public class ConnectionReferencesController : BaseController
    {
        public ConnectionReferencesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        } 

        [EnableQuery]
        public IQueryable<ConnectionReference> Get()
        {
            return base.dbContext.ConnectionReferences.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ConnectionReference connectionReference)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (dbContext.ConnectionReferences.Any(x => x.MsId == connectionReference.MsId))
            {
                return BadRequest("Connection References already exists with this Ms Id");
            }

            base.dbContext.ConnectionReferences.Add(connectionReference);
            await base.dbContext.SaveChangesAsync();

            return Created(connectionReference);
        }

        [HttpPost]
        public async Task<IEnumerable<ConnectionReference>> GetConnectionReferencesForApplication(ODataActionParameters parameters, [FromServices] ConnectionReferenceService connectionReferenceService)
        {
            int applicationId = (int)parameters["applicationId"];
            var connectionReferences = await connectionReferenceService.GetExistsingConnectionReferencesFromDataverse(applicationId);

            return connectionReferences;
        }
    }
}