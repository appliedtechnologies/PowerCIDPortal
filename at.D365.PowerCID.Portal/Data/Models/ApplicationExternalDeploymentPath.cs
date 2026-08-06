using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Data.Models
{
    /// <summary>
    /// Join entity mapping an <see cref="Application"/> to one of the <see cref="ExternalDeploymentPath"/>s it can
    /// be delivered through. Mirrors <see cref="ApplicationDeploymentPath"/> for the external delivery area.
    /// </summary>
    public class ApplicationExternalDeploymentPath
    {
        public int Application { get; set; }
        public int ExternalDeploymentPath { get; set; }
        public int? HierarchieNumber { get; set; }

        public virtual Application ApplicationNavigation { get; set; }
        public virtual ExternalDeploymentPath ExternalDeploymentPathNavigation { get; set; }
    }
}
