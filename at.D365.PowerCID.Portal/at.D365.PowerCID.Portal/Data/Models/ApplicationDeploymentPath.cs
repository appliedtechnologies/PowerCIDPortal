using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public class ApplicationDeploymentPath
    {
        public int Application { get; set; }
        public int DeploymentPath { get; set; }
        public int? HierarchieNumber { get; set; }

        public virtual Application ApplicationNavigation { get; set; }
        public virtual DeploymentPath DeploymentPathNavigation { get; set; }
    }
}
