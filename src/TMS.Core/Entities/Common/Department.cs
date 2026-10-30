using TMS.Core.Entities.Identity;

namespace TMS.Core.Entities.Common;

public class Department : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int? ParentDepartmentId { get; set; }
    public string? ManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;

    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public ApplicationUser? ManagerUser { get; set; }
}
