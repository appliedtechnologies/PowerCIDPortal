using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class Environment: ITrackModified
    {
        public Environment()
        {
            Actions = new HashSet<Action>();
            ApplicationDevelopmentEnviromentNavigation = new HashSet<Application>();
        }

        public int Id { get; set; }
        public int? OrdinalNumber { get; set; }
        public string Name { get; set; }
        public string BasicUrl { get; set; }
        public bool IsDevelopmentEnvironment { get; set; }
        public string ConnectionsOwner { get; set; }
        public bool DeployUnmanaged { get; set; }
        public Guid MsId { get; set; }
        public int Tenant { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public virtual ICollection<DeploymentPath> DeloymentPaths { get; set; }
        public virtual ICollection<DeploymentPathEnvironment> DeploymentPathEnvironments { get; set; }

        public virtual ICollection<Application> ApplicationDevelopmentEnviromentNavigation { get; set; }
        public virtual User CreatedByNavigation { get; set; }
        public virtual User ModifiedByNavigation { get; set; }
        public virtual Tenant TenantNavigation { get; set; }
        public virtual ICollection<Action> Actions { get; set; }        
        public virtual ICollection<UserEnvironment> UserEnvironments { get; set; }
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<Publisher> Publishers { get; set; }
        public virtual ICollection<ConnectionReferenceEnvironment> ConnectionReferenceEnvironments { get; set; }
        public virtual ICollection<ConnectionReference> ConnectionReferences { get; set; }
        public virtual ICollection<EnvironmentVariable> EnvironmentVariables { get; set; }
        public virtual ICollection<EnvironmentVariableEnvironment> EnvironmentVariableEnvironments { get; set; }
    }
}
