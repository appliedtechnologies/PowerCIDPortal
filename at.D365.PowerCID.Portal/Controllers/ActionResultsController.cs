using System.Linq;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class ActionResultsController : BaseController
    {
        public ActionResultsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        // GET: odata/ActionResults
        [EnableQuery]
        public IQueryable<at.D365.PowerCID.Portal.Data.Models.ActionResult> Get()
        {
            return base.dbContext.ActionResults;
        }
    }
}
