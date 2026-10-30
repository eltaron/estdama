using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Registrations;

public class RegistrationFormViewModel
{
    public int CourseScheduleId { get; set; }
    public int TraineeId { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;
    public decimal? FeeAmount { get; set; }
    public decimal? AmountPaid { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string? Notes { get; set; }
}