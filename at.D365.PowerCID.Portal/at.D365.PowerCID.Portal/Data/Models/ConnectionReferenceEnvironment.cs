using System;
using at.D365.PowerCID.Portal.Data.Models;

public class ConnectionReferenceEnvironment
{
    public int ConnectionReference { get; set; }
    public int Environment { get; set; }

    public string ConnectionId { get; set; }

    public virtual ConnectionReference ConnectionReferenceNavigation { get; set; }
    public virtual at.D365.PowerCID.Portal.Data.Models.Environment EnvironmentNavigation { get; set; }
}