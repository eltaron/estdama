using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Finance;

public class FinanceTransaction : BaseEntity
{
    public int CourseId { get; set; }
    public FinanceTransactionType Type { get; set; } = FinanceTransactionType.Expense;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string DescriptionAr { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
    public string? CreatedById { get; set; }
    public string? Notes { get; set; }

    public Course Course { get; set; } = null!;
    public ApplicationUser? CreatedBy { get; set; }
}