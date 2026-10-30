using TMS.Core.Entities.Common;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Trainees;

public class Trainee : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public TraineeType Type { get; set; } = TraineeType.Government;
    public string? EmployeeNumber { get; set; }
    public int? CurrentDepartmentId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? JobTitle { get; set; }
    public string? Organization { get; set; }
    public string? Directorate { get; set; }
    public string? JobGrade { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Department? CurrentDepartment { get; set; }
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
