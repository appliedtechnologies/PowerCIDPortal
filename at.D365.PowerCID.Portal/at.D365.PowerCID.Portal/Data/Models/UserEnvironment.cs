using System;
using at.D365.PowerCID.Portal.Data.Models;

public class UserEnvironment
{
    public int User { get; set; }
    public int Environment { get; set; }

    public virtual User UserNavigation { get; set; }
    public virtual at.D365.PowerCID.Portal.Data.Models.Environment EnvironmentNavigation { get; set; }
}