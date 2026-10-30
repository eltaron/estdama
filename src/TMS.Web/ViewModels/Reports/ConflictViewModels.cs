namespace TMS.Web.ViewModels.Reports;

public class ScheduleConflictItem
{
    public string ResourceName { get; set; } = "";
    public string CourseAName { get; set; } = "";
    public string ScheduleATimes { get; set; } = "";
    public string CourseBName { get; set; } = "";
    public string ScheduleBTimes { get; set; } = "";
}

public class EquipmentConflictItem
{
    public string EquipmentName { get; set; } = "";
    public string CourseAName { get; set; } = "";
    public string LoanAPeriod { get; set; } = "";
    public string CourseBName { get; set; } = "";
    public string LoanBPeriod { get; set; } = "";
}

public class ConflictListViewModel
{
    public List<ScheduleConflictItem> InstructorConflicts { get; set; } = new();
    public List<ScheduleConflictItem> RoomConflicts { get; set; } = new();
    public List<EquipmentConflictItem> EquipmentConflicts { get; set; } = new();
    public int Total => InstructorConflicts.Count + RoomConflicts.Count + EquipmentConflicts.Count;
}