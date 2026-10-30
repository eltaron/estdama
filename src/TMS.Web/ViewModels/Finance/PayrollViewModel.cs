using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Trainees;

namespace TMS.Web.ViewModels.Finance;

public class InstructorPayrollRow
{
    public Instructor Instructor { get; set; } = null!;
    public int ScheduleCount { get; set; }
    public decimal TotalHours { get; set; }
    public decimal GrossAmount { get; set; }
}

public class PayrollViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<InstructorPayrollRow> Rows { get; set; } = new();
    public decimal TotalHours { get; set; }
    public decimal TotalGross { get; set; }

    public List<CourseAccountsCard> CourseCards { get; set; } = new();
}

public class CourseAccountsCard
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int TraineeCount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal MinistryDeduction { get; set; }
    public decimal NetAfterMinistry { get; set; }
}