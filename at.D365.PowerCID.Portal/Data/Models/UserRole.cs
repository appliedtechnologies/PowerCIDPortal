using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class UserRole
    {
        public string Id { get; set; }
        public string ResourceId { get; set; }

        public string AppRoleId { get; set; }

    }
}