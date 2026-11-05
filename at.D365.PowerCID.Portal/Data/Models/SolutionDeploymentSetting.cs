using System;
using System.Collections.Generic;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public class SolutionDeploymentSetting
    {
        public SolutionDeploymentSetting()
        {
            Values = new HashSet<SolutionDeploymentSettingValue>();
        }

        public int Id { get; set; }
        public int ManifestId { get; set; }
        public DeploymentSettingKind Kind { get; set; }
        public Guid MsId { get; set; }
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string ConnectorId { get; set; }
        public string EnvironmentVariableType { get; set; }
        public string DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public string ComponentHash { get; set; }

        public virtual SolutionDeploymentManifest ManifestNavigation { get; set; }
        public virtual ICollection<SolutionDeploymentSettingValue> Values { get; set; }
    }
}
