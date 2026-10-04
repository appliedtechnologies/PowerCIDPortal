using System;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public class SolutionDeploymentSettingValue
    {
        public int Id { get; set; }
        public int SettingId { get; set; }
        public int EnvironmentId { get; set; }
        public string Value { get; set; }
        public bool IsConfigured { get; set; }
        public bool IsInherited { get; set; }
        public int? InheritedFromSolutionId { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public byte[] RowVersion { get; set; }

        public virtual SolutionDeploymentSetting SettingNavigation { get; set; }
        public virtual Environment EnvironmentNavigation { get; set; }
        public virtual Solution InheritedFromSolutionNavigation { get; set; }
        public virtual User ModifiedByNavigation { get; set; }
    }
}
