using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;

namespace TMS.Core.Entities.Instructors;

public class InstructorEvaluation : BaseEntity
{
    public int InstructorId { get; set; }
    public int CourseScheduleId { get; set; }
    public decimal AverageRating { get; set; }
    public int EvaluationCount { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public Instructor Instructor { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
}
