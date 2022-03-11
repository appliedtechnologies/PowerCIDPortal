using System.Linq;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class ActionsController : BaseController
    {
        public ActionsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        [EnableQuery]
        public IQueryable<Action> Get([FromODataUri] int key)
        {
            return base.dbContext.Actions.Where(e => e.TargetEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser && e.Id == key);
        }

        // GET: odata/Actions
        [EnableQuery]
        public IQueryable<at.D365.PowerCID.Portal.Data.Models.Action> Get()
        {
            return base.dbContext.Actions.Where(e => e.TargetEnvironmentNavigation.TenantNavigation.MsId == this.msIdTenantCurrentUser);
        }
    }
}
