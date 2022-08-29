using System.Linq;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class ActionResultsController : BaseController
    {
        private readonly ILogger logger;

        public ActionResultsController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ActionResultsController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
            this.logger = logger;
        }

        // GET: odata/ActionResults
        [EnableQuery]
        public IQueryable<at.D365.PowerCID.Portal.Data.Models.ActionResult> Get()
        {
            logger.LogDebug("Begin: ActionResultsController Get()");
            
            return base.dbContext.ActionResults;
        }
    }
}
