using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class Action : ITrackCreated
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TargetEnvironment { get; set; }
        public int Type { get; set; }
        public int? Status { get; set; }
        public int? Result { get; set; }
        public DateTime? StartTime { get; set; }
        public int? Solution { get; set; }
        public string ErrorMessage { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? FinishTime { get; set; }
        public bool? ExportOnly { get; set; }
        public int? ImportTargetEnvironment { get; set; }

        public virtual User CreatedByNavigation { get; set; }
        public virtual Solution SolutionNavigation { get; set; }
        public virtual ActionResult ResultNavigation { get; set; }
        public virtual ActionStatus StatusNavigation { get; set; }
        public virtual Environment TargetEnvironmentNavigation { get; set; }
        public virtual ActionType TypeNavigation { get; set; }
        public virtual ICollection<AsyncJob> AsyncJobs { get; set; }
    }
}
