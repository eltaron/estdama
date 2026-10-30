using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Reports;

public class CourseReportRow
{
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? CategoryName { get; set; }
    public int Hours { get; set; }
    public CourseStatus Status { get; set; }
    public int ScheduleCount { get; set; }
    public int RegistrationCount { get; set; }
}

public class RegistrationReportRow
{
    public string TraineeName { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string InstructorName { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public RegistrationStatus Status { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? AmountPaid { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}

public class FinanceReportRow
{
    public string CourseName { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Registrations { get; set; }
    public decimal? TotalFees { get; set; }
    public decimal? TotalPaid { get; set; }
    public decimal? Outstanding { get; set; }
}

public class AttendanceReportRow
{
    public string CourseName { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Records { get; set; }
    public int Sessions { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int AttendanceRate { get; set; }
}

public class ExamReportRow
{
    public string CourseName { get; set; } = "";
    public string TitleAr { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public DateTime? ExamDate { get; set; }
    public int? TotalMarks { get; set; }
    public int? PassingMarks { get; set; }
    public int Submissions { get; set; }
    public decimal AvgScore { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int PassRate { get; set; }
}

public class InstructorReportRow
{
    public string InstructorName { get; set; } = "";
    public int ScheduleCount { get; set; }
    public int CourseCount { get; set; }
    public int TotalDays { get; set; }
    public int TotalRegistrations { get; set; }
}

public class TraineeReportRow
{
    public int TraineeId { get; set; }
    public string TraineeNameAr { get; set; } = "";
    public string TraineeNameEn { get; set; } = "";
    public TraineeType TraineeType { get; set; }
    public string CourseCategoryName { get; set; } = "";
    public string CourseNameAr { get; set; } = "";
    public string CourseNameEn { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool HasGrade { get; set; }
    public decimal ScorePct { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public string? GradeKey { get; set; }
}

public class NameCount
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class TraineeReportViewModel
{
    public List<TraineeReportRow> Rows { get; set; } = new();
    public int TotalTrainees { get; set; }
    public int TotalCourses { get; set; }
    public int GradedCount { get; set; }
    public int PassedCount { get; set; }
    public int PassRate { get; set; }
    public decimal AvgScore { get; set; }
    public List<NameCount> ByCategory { get; set; } = new();
    public List<NameCount> ByTraineeType { get; set; } = new();
    public List<NameCount> ByGrade { get; set; } = new();
}