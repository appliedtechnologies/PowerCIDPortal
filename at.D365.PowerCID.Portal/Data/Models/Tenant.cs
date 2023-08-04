using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class Tenant
    {
        public Tenant()
        {
            Environments = new HashSet<Environment>();
            Users = new HashSet<User>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public Guid MsId { get; set; }
        
        public int GitHubInstallationId { get; set; }
        public string GitHubRepositoryName { get; set; }
        public bool DisablePatchCreation { get; set; }

        public virtual ICollection<DeploymentPath> DeploymentPaths { get; set; }
        public virtual ICollection<Environment> Environments { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
