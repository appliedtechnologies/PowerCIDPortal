using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    /// <summary>
    /// Registers an existing <see cref="Data.Models.Environment"/> (typically belonging to a foreign/customer
    /// tenant) as a valid target for cross-tenant solution delivery.
    /// </summary>
    public partial class ExternalEnvironment : ITrackModified
    {
        public ExternalEnvironment()
        {
            ExternalDeploymentPaths = new HashSet<ExternalDeploymentPath>();
            ExternalDeploymentPathEnvironments = new HashSet<ExternalDeploymentPathEnvironment>();
        }

        public int Id { get; set; }
        public int Environment { get; set; }
        public string Alias { get; set; }
        public bool IsDeactive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public virtual at.D365.PowerCID.Portal.Data.Models.Environment EnvironmentNavigation { get; set; }
        public virtual User CreatedByNavigation { get; set; }
        public virtual User ModifiedByNavigation { get; set; }

        public virtual ICollection<ExternalDeploymentPath> ExternalDeploymentPaths { get; set; }
        public virtual ICollection<ExternalDeploymentPathEnvironment> ExternalDeploymentPathEnvironments { get; set; }
    }
}
