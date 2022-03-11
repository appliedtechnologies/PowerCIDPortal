using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Controllers
{
    [Authorize]
    public class PublisherController : BaseController
    {
        public PublisherController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }
        // GET: odata/Publishers
        [EnableQuery]
        public IQueryable<at.D365.PowerCID.Portal.Data.Models.Publisher> Get()
        {
            return base.dbContext.Publishers.Where(e => e.EnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }
    }
}
