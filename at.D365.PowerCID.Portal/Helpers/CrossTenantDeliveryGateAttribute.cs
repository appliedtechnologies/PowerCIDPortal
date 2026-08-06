using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace at.D365.PowerCID.Portal.Helpers
{
    /// <summary>
    /// Cross-tenant solution delivery is only available to the single ("vendor") tenant configured via
    /// "CrossTenantDelivery:AllowedTenantId" in appsettings.json. This helper centralizes that check so it can be
    /// used both from an action filter (<see cref="CrossTenantDeliveryGateAttribute"/>) and from within controller
    /// actions/services that need a programmatic check.
    /// </summary>
    public static class CrossTenantDeliveryHelper
    {
        public static bool IsTenantAllowed(IConfiguration configuration, Guid tenantMsId)
        {
            string allowedTenantId = configuration["CrossTenantDelivery:AllowedTenantId"];

            return !string.IsNullOrEmpty(allowedTenantId)
                && Guid.TryParse(allowedTenantId, out Guid allowedTenantGuid)
                && allowedTenantGuid == tenantMsId;
        }
    }

    /// <summary>
    /// Restricts the decorated controller/action to the single tenant that cross-tenant delivery is enabled for.
    /// Returns 403 Forbidden for every other tenant, and also when the setting is not configured at all
    /// (the feature is disabled by default).
    /// </summary>
    public class CrossTenantDeliveryGateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IConfiguration configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            string tenantIdClaim = context.HttpContext.User.FindFirst(ClaimConstants.TenantId)?.Value;

            if (!Guid.TryParse(tenantIdClaim, out Guid currentTenantGuid)
                || !CrossTenantDeliveryHelper.IsTenantAllowed(configuration, currentTenantGuid))
            {
                context.Result = new ForbidResult();
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
