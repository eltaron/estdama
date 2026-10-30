using TMS.Core.Entities.Finance;
using TMS.Core.Entities.Courses;

namespace TMS.Web.ViewModels.Finance;

public class CourseAccountsViewModel
{
    public Course Course { get; set; } = null!;

    public int TraineeCount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalUnpaid { get; set; }

    public decimal InstructorCost { get; set; }
    public decimal MinistryDeduction { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }

    public List<FinanceTransaction> Transactions { get; set; } = new();
    public List<TraineeStatementRow> TraineeStatements { get; set; } = new();
}

public class TraineeStatementRow
{
    public string TraineeName { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? AmountPaid { get; set; }
    public string PaymentStatusLabel { get; set; } = string.Empty;
}