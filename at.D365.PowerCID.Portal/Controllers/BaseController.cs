using System;
using Microsoft.AspNetCore.Authorization;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Linq;

namespace at.D365.PowerCID.Portal.Controllers
{    
    [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager, atPowerCID.User, atPowerCID.ExternalReleaseManager, atPowerCID.ExternalDeployer")]
    public abstract class BaseController : ODataController
    {
        protected atPowerCIDContext dbContext;
        protected IDownstreamApi downstreamWebApi;
        protected ITokenAcquisition tokenAcquisition;
        protected Guid msIdTenantCurrentUser;
        protected Guid msIdCurrentUser;
        protected IConfiguration configuration;

        
        public BaseController(atPowerCIDContext atPowerCIDContext, IDownstreamApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ITokenAcquisition tokenAcquisition = null, IConfiguration configuration = null)
        {
            dbContext = atPowerCIDContext;
            this.downstreamWebApi = downstreamWebApi;
            this.tokenAcquisition = tokenAcquisition;
            this.configuration = configuration ?? httpContextAccessor.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            this.msIdTenantCurrentUser = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirst(ClaimConstants.TenantId).Value);
            this.msIdCurrentUser = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirst(ClaimConstants.ObjectId).Value);
            this.dbContext.MsIdCurrentUser = this.msIdCurrentUser;
        }

        protected bool HasRole(string role) => HttpContext?.User?.IsInRole(role) == true;

        protected bool IsCrossTenantVendor()
            => CrossTenantDeliveryHelper.IsTenantAllowed(configuration, msIdTenantCurrentUser);

        protected bool IsExternalRole()
            => HasRole("atPowerCID.Admin") || HasRole("atPowerCID.ExternalDeployer") || HasRole("atPowerCID.ExternalReleaseManager");

        protected bool IsAuthorizedExternalTenant(int tenantId)
        {
            if (!IsCrossTenantVendor() || !IsExternalRole())
                return false;

            if (HasRole("atPowerCID.Admin"))
                return dbContext.Tenants.Any(t => t.Id == tenantId && t.MsId != msIdTenantCurrentUser);

            var userId = dbContext.Users.Where(u => u.MsId == msIdCurrentUser).Select(u => u.Id).FirstOrDefault();
            return userId != 0 && dbContext.UserExternalTenants.Any(x => x.User == userId && x.Tenant == tenantId);
        }

        protected bool CanAccessConfiguration(int applicationId, int environmentId)
        {
            var application = dbContext.Applications.FirstOrDefault(a => a.Id == applicationId);
            var environment = dbContext.Environments.FirstOrDefault(e => e.Id == environmentId);
            if (application == null || environment == null)
                return false;

            var applicationTenant = application.DevelopmentEnvironmentNavigation.TenantNavigation.MsId;
            if (applicationTenant != msIdTenantCurrentUser)
                return false;
            if (environment.TenantNavigation.MsId == msIdTenantCurrentUser)
                return true;

            return dbContext.ExternalEnvironments.Any(x => x.Environment == environmentId)
                && IsAuthorizedExternalTenant(environment.Tenant)
                && !environment.IsDeactive;
        }

        protected bool CanViewExternalAction(Data.Models.Action action)
        {
            if (!action.IsExternalDelivery || !IsCrossTenantVendor() || !IsExternalRole())
                return false;

            var target = action.TargetEnvironmentNavigation;
            return target != null && IsAuthorizedExternalTenant(target.Tenant);
        }

        protected IQueryable<int> AuthorizedExternalTenantIds()
        {
            if (!IsCrossTenantVendor() || !IsExternalRole())
                return dbContext.Tenants.Where(t => false).Select(t => t.Id);
            if (HasRole("atPowerCID.Admin"))
                return dbContext.Tenants.Where(t => t.MsId != msIdTenantCurrentUser).Select(t => t.Id);
            var userId = dbContext.Users.Where(u => u.MsId == msIdCurrentUser).Select(u => u.Id).FirstOrDefault();
            return dbContext.UserExternalTenants.Where(x => x.User == userId).Select(x => x.Tenant);
        }
    }
}
