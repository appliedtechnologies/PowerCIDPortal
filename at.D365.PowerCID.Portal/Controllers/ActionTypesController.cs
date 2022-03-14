using System.Linq;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Identity.Web;

namespace at.D365.PowerCID.Portal.Controllers
{
    public class ActionTypesController : BaseController
    {
        public ActionTypesController(atPowerCIDContext atPowerCIDContext, IDownstreamWebApi downstreamWebApi, IHttpContextAccessor httpContextAccessor) : base(atPowerCIDContext, downstreamWebApi, httpContextAccessor)
        {
        }

        // GET: api/ActionTypes
        [EnableQuery]
        public IQueryable<ActionType> Get()
        {
            return base.dbContext.ActionTypes;
        }
    }
}
