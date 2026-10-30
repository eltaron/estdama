using System.ComponentModel.DataAnnotations;

namespace TMS.Web.ViewModels.Users;

public class ResetPasswordViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Common.Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [MinLength(6, ErrorMessage = "كلمة المرور على الأقل 6 أحرف")]
    [DataType(DataType.Password)]
    [Display(Name = "Common.NewPassword")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين")]
    [Display(Name = "Common.ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}