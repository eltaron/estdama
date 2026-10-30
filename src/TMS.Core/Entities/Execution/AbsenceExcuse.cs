using TMS.Core.Entities.Common;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Execution;

public class AbsenceExcuse : BaseEntity
{
    public int AttendanceRecordId { get; set; }
    public ExcuseType ExcuseType { get; set; }
    public string? Description { get; set; }
    public string? DocumentPath { get; set; }
    public string? ApprovedById { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    public Attendance AttendanceRecord { get; set; } = null!;
    public ApplicationUser? ApprovedBy { get; set; }
}
