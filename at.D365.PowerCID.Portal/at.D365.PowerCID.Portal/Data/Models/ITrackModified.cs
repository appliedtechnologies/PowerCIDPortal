using System;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public interface ITrackModified: ITrackCreated{
        int ModifiedBy { get; set; }
        DateTime ModifiedOn { get; set; }
    }
}