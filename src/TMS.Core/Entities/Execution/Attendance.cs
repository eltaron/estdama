using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Execution;

public class Attendance : BaseEntity
{
    public int CourseScheduleId { get; set; }
    public int TraineeId { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public string? QrCode { get; set; }
    public string? RecordedById { get; set; }
    public string? Notes { get; set; }

    public CourseSchedule CourseSchedule { get; set; } = null!;
    public Trainee Trainee { get; set; } = null!;
    public ApplicationUser? RecordedBy { get; set; }
}
