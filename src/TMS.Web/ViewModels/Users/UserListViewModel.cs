namespace TMS.Web.ViewModels.Users;

public class UserListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? DepartmentName { get; set; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public string? RoleNameAr { get; set; }
}