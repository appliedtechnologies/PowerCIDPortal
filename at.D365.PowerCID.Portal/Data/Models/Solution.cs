using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public abstract partial class Solution: ITrackModified
    {

        public Solution()
        {
            Actions = new HashSet<Action>();
        }

        public int Id { get; set; }
        public Guid MsId { get; set; }
        public string Name { get; set; }
        public int Application { get; set; }
        public string Version { get; set; }
        public string UrlMakerportal { get; set; }
        public string UniqueName { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public bool? OverwriteUnmanagedCustomizations { get; set; }
        public bool? EnableWorkflows { get; set; }

        public virtual Application ApplicationNavigation { get; set; }
        public virtual User CreatedByNavigation { get; set; }
        public virtual User ModifiedByNavigation { get; set; }
        public virtual ICollection<Action> Actions { get; set; }

        public bool IsPatch(){
            return this.GetType().Name.Contains("Patch");
        }
    }
}
