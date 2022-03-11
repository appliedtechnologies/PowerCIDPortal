using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class DeploymentPath : ITrackModified
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

        public virtual ICollection<at.D365.PowerCID.Portal.Data.Models.Environment> Environments { get; set; }
        public virtual ICollection<DeploymentPathEnvironment> DeploymentPathEnvironments { get; set; }

        public virtual ICollection<Application> Applications { get; set; }
        public virtual ICollection<ApplicationDeploymentPath> ApplicationDeploymentPaths { get; set; }

    }
}
