using System.ComponentModel.DataAnnotations;

namespace TMS.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Login.RememberMe")]
    public bool RememberMe { get; set; }
}
