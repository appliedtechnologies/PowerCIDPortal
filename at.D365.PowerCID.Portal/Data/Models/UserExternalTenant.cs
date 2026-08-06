using System;

namespace at.D365.PowerCID.Portal.Data.Models
{
    /// <summary>
    /// Grants a user permission to deploy externally released solutions into environments belonging to the
    /// referenced (foreign/customer) <see cref="Tenant"/>. Mirrors <see cref="UserEnvironment"/> which grants
    /// per-environment deploy permission for internal deliveries.
    /// </summary>
    public class UserExternalTenant
    {
        public int User { get; set; }
        public int Tenant { get; set; }

        public virtual User UserNavigation { get; set; }
        public virtual Tenant TenantNavigation { get; set; }
    }
}
