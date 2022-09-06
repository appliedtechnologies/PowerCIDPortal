﻿using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class PatchesController : BaseController
    {
        private readonly ILogger logger;
        public PatchesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<PatchesController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        // GET: odata/Patches
        [EnableQuery]
        public IQueryable<Patch> Get()
        {
            logger.LogDebug($"Begin & End: PatchesController Get()");

            return base.dbContext.Patches.Where(e => e.ApplicationNavigation.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }

        public async Task<IActionResult> Post([FromBody] Patch patch)
        {
            logger.LogDebug($"Begin: PatchesController Post(patch Version: {patch.Version})");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((await this.dbContext.Applications.FirstOrDefaultAsync(e => e.Id == patch.Application && e.DevelopmentEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser)) == null)
                return Forbid();

            Application application = this.dbContext.Applications.First(e => e.Id == patch.Application);
            string displayNameDataversePatch = $"{application.Name}_{patch.Name}";
            Solution lastSolution = application.Solutions.OrderByDescending(e => e.CreatedOn).FirstOrDefault();
            if (patch.Version == null)
                patch.Version = lastSolution == null ? VersionHelper.GetNextBuildVersion("1.0.0.0") : VersionHelper.GetNextBuildVersion(lastSolution.Version);

            await this.CreatePatchInDataverse(application.SolutionUniqueName, displayNameDataversePatch, application.DevelopmentEnvironmentNavigation.BasicUrl, patch);
            patch.UrlMakerportal = $"https://make.powerapps.com/environments/{application.DevelopmentEnvironmentNavigation.MsId}/solutions/{patch.MsId}";

            this.dbContext.Patches.Add(patch);
            await this.dbContext.SaveChangesAsync();

            logger.LogDebug($"End: PatchesController Post(patch Version: {patch.Version})");

            return Created(patch);
        }

        private async Task CreatePatchInDataverse(string solutionUniqueName, string displayNameDataversePatch, string basicUrl, Patch patch)
        {
            logger.LogDebug($"Begin: PatchesController CreatePatchInDataverse(solutionUniqueName: {solutionUniqueName}, displayNameDataversePatch: {displayNameDataversePatch} ,basicUrl: {basicUrl}, patch Version: {patch.Version})");

            JObject newSolution = new JObject();
            newSolution.Add("DisplayName", displayNameDataversePatch);
            newSolution.Add("ParentSolutionUniqueName", solutionUniqueName);
            newSolution.Add("VersionNumber", patch.Version);

            StringContent solutionContent = new StringContent(JsonConvert.SerializeObject(newSolution), Encoding.UTF8, mediaType: "application/json");

            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{this.msIdTenantCurrentUser}";
                options.BaseUrl = basicUrl + options.BaseUrl;
                options.RelativePath = "/CloneAsPatch";
                options.HttpMethod = HttpMethod.Post;
                options.Scopes = $"{basicUrl}/.default";
            }, content: solutionContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not create Patch in Dataverse");
            }
            else
            {
                patch.MsId = (Guid)(await response.Content.ReadAsAsync<JObject>())["SolutionId"];
                patch.UniqueName = await this.GetUniqueSolutioNamePatchFromDataverse(basicUrl, patch.MsId);
            }
            logger.LogDebug($"End: PatchesController CreatePatchInDataverse(solutionUniqueName: {solutionUniqueName}, displayNameDataversePatch: {displayNameDataversePatch} ,basicUrl: {basicUrl}, patch Version: {patch.Version})");
        }

        private async Task<string> GetUniqueSolutioNamePatchFromDataverse(string basicUrl, Guid msId)
        {
            logger.LogDebug($"Begin: PatchesController GetUniqueSolutioNamePatchFromDataverse(basicUrl: {basicUrl}, msId: {msId.ToString()}");

            var response = await downstreamWebApi.CallWebApiForAppAsync("DataverseApi", options =>
            {
                options.Tenant = $"{this.msIdTenantCurrentUser}";
                options.BaseUrl = basicUrl + options.BaseUrl;
                options.RelativePath = $"/solutions({msId})";
                options.HttpMethod = HttpMethod.Get;
                options.Scopes = $"{basicUrl}/.default";
            });

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Could not get unique solution name of Patch in Dataverse");
            }
            else
            {
                logger.LogDebug($"End: PatchesController GetUniqueSolutioNamePatchFromDataverse(basicUrl: {basicUrl}, msId: {msId.ToString()}");

                return (string)(await response.Content.ReadAsAsync<JObject>())["uniquename"];
            }
        }
    }
}
