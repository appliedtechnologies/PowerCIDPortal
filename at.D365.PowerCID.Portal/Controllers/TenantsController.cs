﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using at.D365.PowerCID.Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Octokit;
using Microsoft.Extensions.Logging;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class TenantsController : BaseController
    {
        private readonly ILogger logger;
        public TenantsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<TenantsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        // GET: odata/Tenants
        [EnableQuery]
        public IQueryable<Tenant> Get()
        {
            logger.LogDebug($"Begin & End: TenantsController Get()");

            return base.dbContext.Tenants.Where(e => e.MsId == this.msIdTenantCurrentUser);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        public async Task<IActionResult> Patch([FromODataUri] int key, Delta<Tenant> tenant)
        {
            logger.LogDebug($"Begin: TenantsController Patch(key: {key}, tenant GetChangedPropertyNames: {tenant.GetChangedPropertyNames().ToString()} )");

            if ((await this.dbContext.Tenants.FindAsync(key)).MsId != this.msIdTenantCurrentUser)
                return Forbid();

            string[] propertyNamesAllowedToChange = { nameof(Tenant.GitHubInstallationId), nameof(Tenant.GitHubRepositoryName), nameof(Tenant.DisablePatchCreation) };
            if (tenant.GetChangedPropertyNames().Except(propertyNamesAllowedToChange).Count() != 0)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var entity = await base.dbContext.Tenants.FindAsync(key);
            if (entity == null)
            {
                return NotFound();
            }
            tenant.Patch(entity);
            try
            {
                await base.dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TenantExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            logger.LogDebug($"End: TenantsController Patch(key: {key}, tenant GetChangedPropertyNames: {tenant.GetChangedPropertyNames().ToString()} )");

            return Updated(entity);
        }

        [Authorize(Roles = "atPowerCID.Admin")]
        [HttpPost]
        public async Task<IActionResult> GetGitHubRepositories([FromODataUri] int key, [FromServices] GitHubService gitHubService)
        {
            logger.LogDebug($"Begin: TenantsController GetGitHubRepositories(key: {key})");

            if ((await this.dbContext.Tenants.FindAsync(key)).MsId != this.msIdTenantCurrentUser)
                return Forbid();

            Tenant tenant = this.dbContext.Tenants.First(e => e.Id == key);

            if (tenant.MsId != this.msIdTenantCurrentUser)
                return Forbid();

            if (tenant.GitHubInstallationId == 0)
                return BadRequest();

            (var installation, var installationClient) = await gitHubService.GetInstallationWithClient(tenant.GitHubInstallationId);

            var repoNames = new List<string>();
            var repoReponse = await installationClient.Connection.GetResponse<ICollection<Repository>>(new Uri("/installation/repositories", UriKind.Relative));
            dynamic repoResonseDeserialized = JsonConvert.DeserializeObject((string)repoReponse.HttpResponse.Body);
            var repos = repoResonseDeserialized["repositories"];

            foreach (dynamic repo in repos)
            {
                repoNames.Add((string)repo["full_name"]);
            }
            logger.LogDebug($"End: TenantsController GetGitHubRepositories(key: {key})");

            return Ok(repoNames);
        }

        private bool TenantExists(int key)
        {
            logger.LogDebug($"Begin & End: TenantsController TenantExists(key: {key})");

            return base.dbContext.Tenants.Any(p => p.Id == key);
        }
    }
}
