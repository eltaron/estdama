using TMS.Core.Entities.Common;
using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Resources;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Courses;

public class CourseSchedule : BaseEntity
{
    public int CourseId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int? RoomId { get; set; }
    public int InstructorId { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
    public string? Notes { get; set; }

    public Course Course { get; set; } = null!;
    public Room? Room { get; set; }
    public Instructor Instructor { get; set; } = null!;
}
