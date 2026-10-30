using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;

namespace TMS.Core.Entities.Execution;

public class Certificate : BaseEntity
{
    public string SerialNumber { get; set; } = string.Empty;
    public int TraineeId { get; set; }
    public int CourseScheduleId { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public string? FilePath { get; set; }
    public string? IssuedById { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevocationReason { get; set; }

    public Trainee Trainee { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ApplicationUser? IssuedBy { get; set; }
}
