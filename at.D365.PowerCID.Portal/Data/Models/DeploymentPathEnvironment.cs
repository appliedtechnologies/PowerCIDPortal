using System;


namespace at.D365.PowerCID.Portal.Data.Models
{
    public class DeploymentPathEnvironment
    {
        public ulong Id { 
            get { 
                ulong id = this.DeploymentPath > this.Environment ? (uint)this.Environment | ((ulong) this.DeploymentPath << 32) :  
                       (uint)this.DeploymentPath | ((ulong)this.Environment << 32);
                return id; 
            } 
        }
        public int DeploymentPath { get; set; }
        public int Environment { get; set; }
        public int StepNumber { get; set; }

        public virtual DeploymentPath DeploymentPathNavigation { get; set; }

        public virtual at.D365.PowerCID.Portal.Data.Models.Environment EnvironmentNavigation { get; set; }

    }
}