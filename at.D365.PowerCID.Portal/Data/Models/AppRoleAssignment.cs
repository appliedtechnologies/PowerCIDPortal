using System;
using System.Collections.Generic;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Models
{
    public partial class AppRoleAssignment
    {
        public string Id { get; set; }
        public Guid AppRoleId { get; set; }
    }
}