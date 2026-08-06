using System;

namespace at.D365.PowerCID.Portal.Data.Models
{
    /// <summary>
    /// An ordered step of an <see cref="ExternalDeploymentPath"/>, pointing at a registered
    /// <see cref="ExternalEnvironment"/>.
    /// </summary>
    public class ExternalDeploymentPathEnvironment
    {
        public ulong Id
        {
            get
            {
                ulong id = this.ExternalDeploymentPath > this.ExternalEnvironment ? (uint)this.ExternalEnvironment | ((ulong)this.ExternalDeploymentPath << 32) :
                       (uint)this.ExternalDeploymentPath | ((ulong)this.ExternalEnvironment << 32);
                return id;
            }
        }
        public int ExternalDeploymentPath { get; set; }
        public int ExternalEnvironment { get; set; }
        public int StepNumber { get; set; }

        public virtual ExternalDeploymentPath ExternalDeploymentPathNavigation { get; set; }

        public virtual ExternalEnvironment ExternalEnvironmentNavigation { get; set; }

    }
}
