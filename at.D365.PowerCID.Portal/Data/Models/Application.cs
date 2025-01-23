using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class Application : ITrackModified
    {
        public Application()
        {
            Solutions = new HashSet<Solution>();
        }

        public int Id { get; set; }
        public int? OrdinalNumber { get; set; }
        public string Name { get; set; }
        public Guid? MsId { get; set; }
        public string SolutionUniqueName { get; set; }
        public int DevelopmentEnvironment { get; set; }
        public int Publisher { get; set; }
        public string InternalDescription { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public bool IsDeactive { get; set; }
        public bool ForceManagedDeployment { get; set; }
        public string AfterDeploymentInformation { get; set; }

        public virtual Environment DevelopmentEnvironmentNavigation { get; set; }

        public virtual Publisher PublisherNavigation { get; set; }

        public virtual User CreatedByNavigation { get; set; }
        public virtual User ModifiedByNavigation { get; set; }
        public virtual ICollection<Solution> Solutions { get; set; }

        public virtual ICollection<at.D365.PowerCID.Portal.Data.Models.DeploymentPath> DeploymentPaths { get; set; }
        public virtual ICollection<ApplicationDeploymentPath> ApplicationDeploymentPaths { get; set; }
        public virtual ICollection<ConnectionReference> ConnectionReferences { get; set; }
        public virtual ICollection<EnvironmentVariable> EnvironmentVariables { get; set; }
    }
}
