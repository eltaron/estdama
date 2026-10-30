using System.ComponentModel.DataAnnotations;
using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Trainees;

public class TraineeFormViewModel
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    [Display(Name = "TraineeForm.Type")]
    public TraineeType Type { get; set; } = TraineeType.Government;

    [Display(Name = "TraineeForm.FullNameAr")]
    [Required(ErrorMessageResourceName = "TraineeForm_FullNameArRequired", ErrorMessageResourceType = typeof(Shared))]
    public string FullNameAr { get; set; } = string.Empty;

    [Display(Name = "TraineeForm.FullNameEn")]
    public string FullNameEn { get; set; } = string.Empty;

    [Display(Name = "TraineeForm.Email")]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    [Display(Name = "TraineeForm.NationalId")]
    public string? NationalId { get; set; }

    [Display(Name = "TraineeForm.Phone")]
    public string? Phone { get; set; }

    [Display(Name = "TraineeForm.Address")]
    public string? Address { get; set; }

    [Display(Name = "TraineeForm.JobTitle")]
    public string? JobTitle { get; set; }

    [Display(Name = "TraineeForm.Organization")]
    public string? Organization { get; set; }

    [Display(Name = "TraineeForm.Directorate")]
    public string? Directorate { get; set; }

    [Display(Name = "TraineeForm.JobGrade")]
    public string? JobGrade { get; set; }

    [Display(Name = "TraineeForm.Notes")]
    public string? Notes { get; set; }
}