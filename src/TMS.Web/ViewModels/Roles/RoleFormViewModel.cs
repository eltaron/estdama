using System.ComponentModel.DataAnnotations;
using TMS.Core.Security;

namespace TMS.Web.ViewModels.Roles;

public class RoleFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "الاسم الفني للدور مطلوب")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "الاسم الفني بالإنجليزية حروف وأرقام فقط")]
    [Display(Name = "RoleForm.Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "الاسم بالعربي مطلوب")]
    [Display(Name = "RoleForm.NameAr")]
    public string NameAr { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "RoleForm.Description")]
    public string? Description { get; set; }

    public List<string> GrantedPermissions { get; set; } = new();

    public List<string> PanelPermissions { get; set; } = new();

    public IEnumerable<PermissionModule> Modules => PermissionCatalog.Modules;
}