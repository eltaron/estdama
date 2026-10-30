using TMS.Core.Entities.Common;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Courses;

public class Course : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? ObjectivesAr { get; set; }
    public string? ObjectivesEn { get; set; }
    public int? CategoryId { get; set; }
    public TargetAudience TargetAudience { get; set; } = TargetAudience.All;
    public int Hours { get; set; }
    public int MaxTrainees { get; set; }
    public int MinTrainees { get; set; }
    public CourseStatus Status { get; set; } = CourseStatus.Planned;
    public decimal? FeePerTrainee { get; set; }
    public bool IsActive { get; set; } = true;

    public CourseCategory? Category { get; set; }
    public ICollection<CourseSchedule> Schedules { get; set; } = new List<CourseSchedule>();
    public ICollection<CourseCompetency> CourseCompetencies { get; set; } = new List<CourseCompetency>();
}
