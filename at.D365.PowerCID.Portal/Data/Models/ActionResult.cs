using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class ActionResult
    {
        public ActionResult()
        {
            Actions = new HashSet<Action>();
        }

        public int Id { get; set; }
        public string Result { get; set; }

        public virtual ICollection<Action> Actions { get; set; }
    }
}
