using System.ComponentModel.DataAnnotations;
using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Instructors;

public class InstructorFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessageResourceName = "InstructorForm_FirstNameArRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "InstructorForm.FirstNameAr")]
    public string FirstNameAr { get; set; } = string.Empty;

    [Required]
    [Display(Name = "InstructorForm.LastNameAr")]
    public string LastNameAr { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = "InstructorForm_FirstNameEnRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "InstructorForm.FirstNameEn")]
    public string FirstNameEn { get; set; } = string.Empty;

    [Required]
    [Display(Name = "InstructorForm.LastNameEn")]
    public string LastNameEn { get; set; } = string.Empty;

    [Display(Name = "InstructorForm.NationalId")]
    public string? NationalId { get; set; }

    [EmailAddress]
    [Display(Name = "InstructorForm.Email")]
    public string? Email { get; set; }

    [Phone]
    [Display(Name = "InstructorForm.Phone")]
    public string? Phone { get; set; }

    [Display(Name = "InstructorForm.SpecializationAr")]
    public string? SpecializationAr { get; set; }

    [Display(Name = "InstructorForm.SpecializationEn")]
    public string? SpecializationEn { get; set; }

    [Display(Name = "InstructorForm.Type")]
    public InstructorType Type { get; set; } = InstructorType.External;

    [Required]
    [Range(0, 10000)]
    [Display(Name = "InstructorForm.HourlyRate")]
    public decimal HourlyRate { get; set; }

    [Display(Name = "InstructorForm.BankName")]
    public string? BankName { get; set; }

    [Display(Name = "InstructorForm.IBAN")]
    public string? IBAN { get; set; }

    [Display(Name = "InstructorForm.AccountNumber")]
    public string? AccountNumber { get; set; }
}