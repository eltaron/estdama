using TMS.Core.Entities.Common;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Courses;

public class TrainingPlan : AuditableEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public string? Description { get; set; }
    public TrainingPlanCategory Category { get; set; } = TrainingPlanCategory.GovernmentAdmin;
    public PlanStatus Status { get; set; } = PlanStatus.Draft;
    public string? CreatedById { get; set; }
    public string? ApprovedById { get; set; }

    public ApplicationUser? CreatedBy { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public ICollection<TrainingPlanItem> Items { get; set; } = new List<TrainingPlanItem>();
}
