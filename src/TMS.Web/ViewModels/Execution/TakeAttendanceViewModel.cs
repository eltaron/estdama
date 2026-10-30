using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Execution;

public class TakeAttendanceViewModel
{
    public int CourseScheduleId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;

    public List<AttendanceRow> Rows { get; set; } = new();
}

public class AttendanceRow
{
    public int TraineeId { get; set; }
    public string TraineeName { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public AttendanceStatus? ExistingStatus { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Notes { get; set; }
}