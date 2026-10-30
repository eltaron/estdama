using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Trainees;

public class WaitingList : BaseEntity
{
    public int TraineeId { get; set; }
    public int CourseScheduleId { get; set; }
    public int Position { get; set; }
    public WaitingListStatus Status { get; set; } = WaitingListStatus.Waiting;
    public DateTime? OfferedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public Trainee Trainee { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
}
