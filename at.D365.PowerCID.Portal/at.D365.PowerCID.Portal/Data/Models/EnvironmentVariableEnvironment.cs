using System;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class EnvironmentVariableEnvironment
    {
        public int EnvironmentVariable { get; set; }
        public int Environment { get; set; }

        public string Value { get; set; }

        public virtual EnvironmentVariable EnvironmentVariableNavigation { get; set; }
        public virtual at.D365.PowerCID.Portal.Data.Models.Environment EnvironmentNavigation { get; set; }
    }
}
