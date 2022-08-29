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



namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class ConnectionReferenceEnvironmentsController : BaseController
    {
        private readonly ILogger logger;
        public ConnectionReferenceEnvironmentsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ConnectionReferenceEnvironmentsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        } 

        [EnableQuery]
        public IQueryable<ConnectionReferenceEnvironment> Get()
        {

            logger.LogDebug($"Begin: ConnectionReferenceEnvironmentsController Get()");

            return base.dbContext.ConnectionReferenceEnvironments.Where(e => e.ConnectionReferenceNavigation.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ConnectionReferenceEnvironment connectionReferenceEnvironment)
        {

            logger.LogDebug($"Begin: ConnectionReferenceEnvironmentsController Post(connectionReferenceEnvironment: {connectionReferenceEnvironment.Environment.ToString()})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (dbContext.ConnectionReferenceEnvironments.Any(x => x.ConnectionReference == connectionReferenceEnvironment.ConnectionReference && x.Environment == connectionReferenceEnvironment.Environment))
            {
                return BadRequest("Connection References for this Environment already exists");
            }

            base.dbContext.ConnectionReferenceEnvironments.Add(connectionReferenceEnvironment);
            await base.dbContext.SaveChangesAsync();

            return Created(connectionReferenceEnvironment);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromODataUri] int keyConnectionReference, [FromODataUri] int keyEnvironment, Delta<ConnectionReferenceEnvironment> connectionReferenceEnvironment)
        {
            logger.LogDebug($"Begin: ConnectionReferenceEnvironmentsController Patch(keyConnectionReference: {keyConnectionReference}, keyEnvironment: {keyEnvironment}, connectionReferenceEnvironment: {connectionReferenceEnvironment.GetChangedPropertyNames().Count()} )");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if((await this.dbContext.ConnectionReferences.FirstOrDefaultAsync(e => e.Id == keyConnectionReference && e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            var entity = await base.dbContext.ConnectionReferenceEnvironments.FirstAsync(e => e.ConnectionReference == keyConnectionReference && e.Environment == keyEnvironment);
            if (entity == null)
            {
                return NotFound();
            }
            connectionReferenceEnvironment.Patch(entity);
            try
            {
                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ConnectionReferenceEnvironmentExists(keyConnectionReference, keyEnvironment))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return Updated(entity);
        }

        private bool ConnectionReferenceEnvironmentExists(int keyConnectionReference, int keyEnvironment)
        {
             logger.LogDebug($"Begin: ConnectionReferenceEnvironmentsController Get(keyConnectionReference: {keyConnectionReference}, keyEnvironment: {keyEnvironment})");
            
            return base.dbContext.ConnectionReferenceEnvironments.Any(p => p.ConnectionReference == keyConnectionReference && p.Environment == keyEnvironment);
        }
    }
}