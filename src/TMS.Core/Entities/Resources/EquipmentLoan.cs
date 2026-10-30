using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Resources;

public class EquipmentLoan : BaseEntity
{
    public int EquipmentId { get; set; }
    public int CourseScheduleId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? LoanedById { get; set; }
    public string? ReturnedById { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Loaned;
    public string? Notes { get; set; }

    public Equipment Equipment { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ApplicationUser? LoanedBy { get; set; }
    public ApplicationUser? ReturnedBy { get; set; }
}
