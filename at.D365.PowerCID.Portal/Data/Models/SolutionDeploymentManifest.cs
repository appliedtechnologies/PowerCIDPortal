using System;
using System.Collections.Generic;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public class SolutionDeploymentManifest
    {
        public SolutionDeploymentManifest()
        {
            Settings = new HashSet<SolutionDeploymentSetting>();
        }

        public int Id { get; set; }
        public int SolutionId { get; set; }
        public Guid DataverseSolutionId { get; set; }
        public string DataverseVersion { get; set; }
        public DateTime? DataverseModifiedOn { get; set; }
        public string ManifestHash { get; set; }
        public DeploymentManifestStatus Status { get; set; }
        public DateTime? LastSyncedOn { get; set; }
        public string LastSyncError { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public byte[] RowVersion { get; set; }

        public virtual Solution SolutionNavigation { get; set; }
        public virtual ICollection<SolutionDeploymentSetting> Settings { get; set; }
    }
}
