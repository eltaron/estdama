using System.ComponentModel.DataAnnotations;

namespace TMS.Web.ViewModels.Execution;

public class CertificateFormViewModel
{
    [Required(ErrorMessageResourceName = "CertificateForm_TraineeRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "Certificates.Trainee")]
    public int? TraineeId { get; set; }

    [Required(ErrorMessageResourceName = "CertificateForm_ScheduleRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "Certificates.Schedule")]
    public int? CourseScheduleId { get; set; }

    [Display(Name = "Certificates.IssueDate")]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Certificates.ExpiryDate")]
    public DateTime? ExpiryDate { get; set; }
}