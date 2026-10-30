namespace TMS.Web.ViewModels.Roles;

public class RoleListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
    public bool IsSystemRole { get; set; }
}