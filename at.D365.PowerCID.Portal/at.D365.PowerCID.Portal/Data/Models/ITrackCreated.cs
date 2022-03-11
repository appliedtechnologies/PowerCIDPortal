using System;

namespace at.D365.PowerCID.Portal.Data.Models
{
    public interface ITrackCreated{
        int CreatedBy { get; set; }
        DateTime CreatedOn { get; set; }
    }
}