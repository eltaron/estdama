using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Trainees;

public class Registration : BaseEntity
{
    public int TraineeId { get; set; }
    public int CourseScheduleId { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public string? RegisteredById { get; set; }
    public string? Notes { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? AmountPaid { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public DateTime? PaidAt { get; set; }

    public Trainee Trainee { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
    public ApplicationUser? RegisteredBy { get; set; }
}
