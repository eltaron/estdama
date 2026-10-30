using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Resources;

public class Room : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
    public string? Facilities { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PhotoPath { get; set; }

    public ICollection<RoomBooking> Bookings { get; set; } = new List<RoomBooking>();
}
