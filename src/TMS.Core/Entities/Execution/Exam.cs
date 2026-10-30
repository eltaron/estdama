using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Execution;

public class Exam : BaseEntity
{
    public int CourseScheduleId { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public ExamType Type { get; set; } = ExamType.Final;
    public ExamScope Scope { get; set; } = ExamScope.TheoreticalOnly;
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int PassingMarks { get; set; }
    public bool ShuffleQuestions { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
}
