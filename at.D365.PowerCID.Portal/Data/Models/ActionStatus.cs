using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class ActionStatus
    {
        public ActionStatus()
        {
            Actions = new HashSet<Action>();
        }

        public int Id { get; set; }
        public string Status { get; set; }

        public virtual ICollection<Action> Actions { get; set; }
    }
}
