using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;

namespace TMS.Core.Entities.Resources;

public class RoomBooking : BaseEntity
{
    public int RoomId { get; set; }
    public int CourseScheduleId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? BookedById { get; set; }
    public string? Notes { get; set; }

    public Room Room { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ApplicationUser? BookedBy { get; set; }
}
