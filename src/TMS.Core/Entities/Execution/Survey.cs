using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;

namespace TMS.Core.Entities.Execution;

public class Survey : BaseEntity
{
    public int CourseScheduleId { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
}
