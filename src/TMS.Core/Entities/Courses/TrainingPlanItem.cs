using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Courses;

public class TrainingPlanItem : BaseEntity
{
    public int TrainingPlanId { get; set; }
    public int CourseId { get; set; }
    public int? TargetDepartmentId { get; set; }
    public int? PlannedQuarter { get; set; }
    public string? Notes { get; set; }

    public TrainingPlan TrainingPlan { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Department? TargetDepartment { get; set; }
}
