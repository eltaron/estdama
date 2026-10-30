using TMS.Core.Entities.Courses;
using TMS.Core.Enums;

namespace TMS.Web.ViewModels.TrainingPlans;

public class TrainingPlansReportViewModel
{
    public List<TrainingPlanReportRow> Plans { get; set; } = new();
    public List<int> Years { get; set; } = new();

    public TrainingPlanCategory? Category { get; set; }
    public int? Year { get; set; }
    public int? Quarter { get; set; }
    public PlanStatus? Status { get; set; }
    public string? Search { get; set; }

    public int TotalPlans => Plans.Count;
    public int TotalCourses => Plans.Sum(p => p.TotalCourses);
    public int TotalHours => Plans.Sum(p => p.TotalHours);
    public int TotalSchedules => Plans.Sum(p => p.TotalSchedules);
    public int TotalRegistrations => Plans.Sum(p => p.TotalRegistrations);
}

public class TrainingPlanReportRow
{
    public TrainingPlan Plan { get; set; } = null!;
    public List<TrainingPlanReportItemRow> Items { get; set; } = new();

    public int TotalCourses => Items.Count;
    public int TotalHours => Items.Sum(i => i.Course.Hours);
    public int TotalSchedules => Items.Sum(i => i.ScheduleCount);
    public int TotalRegistrations => Items.Sum(i => i.Registrations);
}

public class TrainingPlanReportItemRow
{
    public Course Course { get; set; } = null!;
    public string? TargetDepartmentName { get; set; }
    public int? PlannedQuarter { get; set; }
    public string? Notes { get; set; }
    public int ScheduleCount { get; set; }
    public DateTime? FirstScheduleStart { get; set; }
    public DateTime? LastScheduleEnd { get; set; }
    public string? Instructors { get; set; }
    public int Registrations { get; set; }
}