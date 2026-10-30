using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Courses;

public class CourseDetailsViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? ObjectivesAr { get; set; }
    public TargetAudience TargetAudience { get; set; }
    public int Hours { get; set; }
    public int MaxTrainees { get; set; }
    public int MinTrainees { get; set; }
    public CourseStatus Status { get; set; }
    public decimal? FeePerTrainee { get; set; }
    public string? CategoryName { get; set; }
    public List<ScheduleListItem> Schedules { get; set; } = new();
}

public class ScheduleListItem
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? RoomName { get; set; }
    public string? InstructorName { get; set; }
    public ScheduleStatus Status { get; set; }
    public int RegisteredCount { get; set; }
}
