using System;

public class AsyncJob
{
    public int Id { get; set; }
    public Guid? AsyncOperationId { get; set; }
    public Guid? JobId { get; set; }
    public bool IsManaged { get; set; }
    public int Action { get; set; }

    public virtual at.D365.PowerCID.Portal.Data.Models.Action ActionNavigation { get; set; }
}
