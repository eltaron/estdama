using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Trainees;

namespace TMS.Core.Entities.Execution;

public class SurveyResponse : BaseEntity
{
    public int SurveyId { get; set; }
    public int TraineeId { get; set; }
    public int CourseScheduleId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsAnonymous { get; set; }

    public Survey Survey { get; set; } = null!;
    public Trainee Trainee { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
