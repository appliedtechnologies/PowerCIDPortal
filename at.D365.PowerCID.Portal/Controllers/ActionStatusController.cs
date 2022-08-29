using System.Linq;
using Microsoft.AspNetCore.Mvc;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class ActionStatusController : BaseController
    {
        private readonly ILogger logger;
        public ActionStatusController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ActionStatusController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
              this.logger = logger;
        }

        // GET: odata/ActionStatus
        [EnableQuery]
        public IQueryable<ActionStatus> Get()
        {
            logger.LogDebug($"Begin: ActionStatusController ActionExists()");
            return base.dbContext.ActionStatuses;
        }
    }
}
