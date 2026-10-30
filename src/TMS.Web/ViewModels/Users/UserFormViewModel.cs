using System.ComponentModel.DataAnnotations;

namespace TMS.Web.ViewModels.Users;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "الاسم بالعربي مطلوب")]
    [StringLength(200)]
    [Display(Name = "UserForm.FullNameAr")]
    public string FullNameAr { get; set; } = string.Empty;

    [Required(ErrorMessage = "English name is required")]
    [StringLength(200)]
    [Display(Name = "UserForm.FullNameEn")]
    public string FullNameEn { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "Common.Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "رقم الجوال غير صحيح")]
    [Display(Name = "Common.PhoneNumber")]
    public string? PhoneNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "Users.EmployeeNumber")]
    public string? EmployeeNumber { get; set; }

    [StringLength(50)]
    [Display(Name = "Common.NationalId")]
    public string? NationalId { get; set; }

    [Display(Name = "Common.Department")]
    public int? DepartmentId { get; set; }

    [StringLength(200)]
    [Display(Name = "Common.JobTitle")]
    public string? JobTitle { get; set; }

    [Required(ErrorMessage = "اختر الدور")]
    [Display(Name = "Common.Role")]
    public string RoleId { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Common.Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Common.ConfirmPassword")]
    public string? ConfirmPassword { get; set; }

    [Display(Name = "Common.IsActive")]
    public bool IsActive { get; set; } = true;
}