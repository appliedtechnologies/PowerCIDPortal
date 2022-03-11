using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class User
    {
        public User()
        {
            Actions = new HashSet<Action>();
            ApplicationCreatedByNavigations = new HashSet<Application>();
            ApplicationModifiedByNavigations = new HashSet<Application>();
            EnvironmentCreatedByNavigations = new HashSet<Environment>();
            EnvironmentModifiedByNavigations = new HashSet<Environment>();
            SolutionCreatedByNavigations = new HashSet<Solution>();
            SolutionModifiedByNavigations = new HashSet<Solution>();
            DeploymentPathCreatedByNavigations = new HashSet<DeploymentPath>();
            DeploymentPathModifiedByNavigations = new HashSet<DeploymentPath>();

        }

        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public Guid MsId { get; set; }
        public int Tenant { get; set; }
        public bool MakeAdmin { get; set; }
        
        public virtual Tenant TenantNavigation { get; set; }
        public virtual ICollection<DeploymentPath> DeploymentPathCreatedByNavigations { get; set; }
        public virtual ICollection<DeploymentPath> DeploymentPathModifiedByNavigations { get; set; }
        
        public virtual ICollection<Action> Actions { get; set; }
        public virtual ICollection<Application> ApplicationCreatedByNavigations { get; set; }
        public virtual ICollection<Application> ApplicationModifiedByNavigations { get; set; }
        public virtual ICollection<Environment> EnvironmentCreatedByNavigations { get; set; }
        public virtual ICollection<Environment> EnvironmentModifiedByNavigations { get; set; }
        public virtual ICollection<Solution> SolutionCreatedByNavigations { get; set; }
        public virtual ICollection<Solution> SolutionModifiedByNavigations { get; set; }
        public virtual ICollection<ConnectionReference> ConnectionReferenceCreatedByNavigation { get; set; }
        public virtual ICollection<ConnectionReference> ConnectionReferenceModifiedByNavigation { get; set; }
        public virtual ICollection<EnvironmentVariable> EnvironmentVariableCreatedByNavigations { get; set; }
        public virtual ICollection<EnvironmentVariable> EnvironmentVariableModifiedByNavigations { get; set; }


        public virtual ICollection<UserEnvironment> UserEnvironments { get; set; }
        public virtual ICollection<Environment> Environments { get; set; }
    }
}
