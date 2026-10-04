namespace at.D365.PowerCID.Portal.Data.Models
{
    public enum DeploymentManifestStatus
    {
        Draft = 1,
        Syncing = 2,
        Ready = 3,
        Stale = 4,
        SyncFailed = 5
    }
}
