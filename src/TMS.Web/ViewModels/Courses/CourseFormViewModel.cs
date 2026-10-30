using System.ComponentModel.DataAnnotations;
using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Courses;

public class CourseFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessageResourceName = "CourseForm_NameArRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "CourseForm.NameAr")]
    public string NameAr { get; set; } = string.Empty;

    [Required(ErrorMessageResourceName = "CourseForm_NameEnRequired", ErrorMessageResourceType = typeof(Shared))]
    [Display(Name = "CourseForm.NameEn")]
    public string NameEn { get; set; } = string.Empty;

    [Display(Name = "CourseForm.DescriptionAr")]
    public string? DescriptionAr { get; set; }

    [Display(Name = "CourseForm.DescriptionEn")]
    public string? DescriptionEn { get; set; }

    [Display(Name = "CourseForm.ObjectivesAr")]
    public string? ObjectivesAr { get; set; }

    [Display(Name = "CourseForm.ObjectivesEn")]
    public string? ObjectivesEn { get; set; }

    [Display(Name = "CourseForm.TargetAudience")]
    public TargetAudience TargetAudience { get; set; } = TargetAudience.All;

    [Required]
    [Range(1, 500)]
    [Display(Name = "CourseForm.Hours")]
    public int Hours { get; set; }

    [Required]
    [Range(1, 500)]
    [Display(Name = "CourseForm.MaxTrainees")]
    public int MaxTrainees { get; set; }

    [Range(1, 500)]
    [Display(Name = "CourseForm.MinTrainees")]
    public int MinTrainees { get; set; }

    [Display(Name = "CourseForm.CategoryId")]
    public int? CategoryId { get; set; }

    [Display(Name = "CourseForm.FeePerTrainee")]
    public decimal? FeePerTrainee { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Planned;
}