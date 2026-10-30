using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Courses;

public class CourseListViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int Hours { get; set; }
    public int MaxTrainees { get; set; }
    public CourseStatus Status { get; set; }
    public int ScheduleCount { get; set; }
}
