using Microsoft.AspNetCore.Identity;
using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public string? NationalId { get; set; }
    public int? DepartmentId { get; set; }
    public string? JobTitle { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    public Department? Department { get; set; }
}
