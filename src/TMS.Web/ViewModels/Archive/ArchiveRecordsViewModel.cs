namespace TMS.Web.ViewModels.Archive;

public class ArchiveRecordsViewModel
{
    public List<ArchivedScheduleItem> Schedules { get; set; } = new();
    public int TotalSchedules { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalCompleted { get; set; }
    public decimal TotalCollected { get; set; }
    public int? Year { get; set; }
    public List<int> AvailableYears { get; set; } = new();
}

public class ArchivedScheduleItem
{
    public int ScheduleId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseNameAr { get; set; }
    public string? CourseNameEn { get; set; }
    public string? InstructorName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int RegistrationCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal Collected { get; set; }
}