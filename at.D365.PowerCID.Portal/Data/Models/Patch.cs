using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class Patch : Solution
    {
        public bool WasDeleted { get; set; }

        public bool IsDeletable { get {
            bool hasSuccessfulImports = this.Actions.Where(e => e.Type == 2 && e.Result == 1).Count() > 0;
            bool hasRunningActions = this.Actions.Where(e => e.Status == 1 || e.Status == 2).Count() > 0;
            return !hasSuccessfulImports && !hasRunningActions;
        } } 
    }
}
