using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class Tenant
    {
        public Tenant()
        {
            Environments = new HashSet<Environment>();
            Users = new HashSet<User>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public Guid MsId { get; set; }
        
        public int GitHubInstallationId { get; set; }
        public string GitHubRepositoryName { get; set; }
        public bool DisablePatchCreation { get; set; }

        public virtual ICollection<DeploymentPath> DeploymentPaths { get; set; }
        public virtual ICollection<Environment> Environments { get; set; }
        public virtual ICollection<User> Users { get; set; }

        /// <summary>Deployment paths owned by this tenant used to ship solutions into foreign tenants (only used by the vendor tenant).</summary>
        public virtual ICollection<ExternalDeploymentPath> ExternalDeploymentPaths { get; set; }
        /// <summary>Permission entries granting users of the vendor tenant the right to deploy into this (foreign) tenant.</summary>
        public virtual ICollection<UserExternalTenant> UserExternalTenants { get; set; }
    }
}
