using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace at.D365.PowerCID.Portal.Data.Models
{
    /// <summary>
    /// A deployment path used to ship externally released solution versions into a chain of
    /// <see cref="ExternalEnvironment"/>s. Owned/managed by the vendor tenant (the only tenant this
    /// cross-tenant delivery feature is enabled for).
    /// </summary>
    public partial class ExternalDeploymentPath : ITrackModified
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Tenant { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public virtual User CreatedByNavigation { get; set; }
        public virtual User ModifiedByNavigation { get; set; }
        public virtual Tenant TenantNavigation { get; set; }

        public virtual ICollection<ExternalEnvironment> ExternalEnvironments { get; set; }
        public virtual ICollection<ExternalDeploymentPathEnvironment> ExternalDeploymentPathEnvironments { get; set; }

        public virtual ICollection<Application> Applications { get; set; }
        public virtual ICollection<ApplicationExternalDeploymentPath> ApplicationExternalDeploymentPaths { get; set; }
    }
}
