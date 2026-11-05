using System;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public class DeploymentSettingSnapshot
    {
        public int Id { get; set; }
        public int ActionId { get; set; }
        public int SolutionId { get; set; }
        public int EnvironmentId { get; set; }
        public DeploymentSettingKind Kind { get; set; }
        public Guid MsId { get; set; }
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string ConnectorId { get; set; }
        public string Value { get; set; }
        public bool IsConfigured { get; set; }
        public DateTime CreatedOn { get; set; }

        public virtual Action ActionNavigation { get; set; }
        public virtual Solution SolutionNavigation { get; set; }
        public virtual Environment EnvironmentNavigation { get; set; }
    }
}
