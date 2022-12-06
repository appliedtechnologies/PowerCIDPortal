using System;
using Microsoft.AspNetCore.Authorization;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Identity.Web;

namespace at.D365.PowerCID.Portal.Controllers
{    
    [Authorize(Roles = "atPowerCID.Admin, atPowerCID.Manager, atPowerCID.User")]
    public abstract class BaseController : ODataController
    {
        protected atPowerCIDContext dbContext;
        protected IDownstreamWebApi downstreamWebApi;
        protected ITokenAcquisition tokenAcquisition;
        protected Guid msIdTenantCurrentUser;
        protected Guid msIdCurrentUser;

        
        public BaseController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ITokenAcquisition tokenAcquisition = null)
        {
            dbContext = atPowerCIDContext;
            this.downstreamWebApi = downstreamWebApi;
            this.tokenAcquisition = tokenAcquisition;
            this.msIdTenantCurrentUser = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirst(ClaimConstants.TenantId).Value);
            this.msIdCurrentUser = Guid.Parse(httpContextAccessor.HttpContext.User.FindFirst(ClaimConstants.ObjectId).Value);
            this.dbContext.MsIdCurrentUser = this.msIdCurrentUser;
        }
    }
}
