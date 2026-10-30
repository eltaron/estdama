namespace TMS.Web.ViewModels.Execution;

public class CheckInViewModel
{
    public string Code { get; set; } = "";
    public string? EmployeeNumber { get; set; }
    public int CourseScheduleId { get; set; }
    public string? CourseName { get; set; }
    public string? TraineeName { get; set; }
    public TimeSpan? CheckInTime { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
    public bool AlreadyMarked { get; set; }
    public int PresentCount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
}