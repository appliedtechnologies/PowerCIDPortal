using System.Linq;
using Microsoft.AspNetCore.Mvc;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Http;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class ActionStatusController : BaseController
    {
        public ActionStatusController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        // GET: odata/ActionStatus
        [EnableQuery]
        public IQueryable<ActionStatus> Get()
        {
            return base.dbContext.ActionStatuses;
        }
    }
}
