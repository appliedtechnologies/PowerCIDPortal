using System;
using System.Collections.Generic;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class ConnectionReference : ITrackModified
    {

        public int Id { get; set; }
        public int Application { get; set; }

        public Guid MsId { get; set; }
        public string LogicalName { get; set; }
        public string ConnectorId { get; set; }
        public string DisplayName { get; set; }

        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ModifiedBy { get; set; }

        public DateTime ModifiedOn { get; set; }

        public virtual User CreatedByNavigation { get; set; }
        public virtual User ModifiedByNavigation { get; set; }
        public virtual Application ApplicationNavigation { get; set; }

        public virtual ICollection<Environment> Environments { get; set; }
        public virtual ICollection<ConnectionReferenceEnvironment> ConnectionReferenceEnvironments { get; set; }
    }
}
