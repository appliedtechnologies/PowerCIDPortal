using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public class Publisher
    {
        public Publisher()
        {
            Applications = new HashSet<Application>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public Guid MsId { get; set; }
        public int Environment { get; set; }

        public virtual ICollection<Application> Applications { get; set; }
        public virtual Environment EnvironmentNavigation { get; set; }
    }
}
