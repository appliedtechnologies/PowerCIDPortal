using System.Linq;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class ActionTypesController : BaseController
    {        
        private readonly ILogger logger;
        public ActionTypesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor, ILogger<ActionTypesController> logger) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
             this.logger = logger;
        }

        // GET: api/ActionTypes
        [EnableQuery]
        public IQueryable<ActionType> Get()
        {
            logger.LogDebug($"Begin: ActionTypesController Get()");
            return base.dbContext.ActionTypes;
        }
    }
}
