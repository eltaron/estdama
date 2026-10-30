using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Execution;

public class BankQuestionOption : BaseEntity
{
    public int BankQuestionId { get; set; }
    public string OptionTextAr { get; set; } = string.Empty;
    public string OptionTextEn { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }

    public BankQuestion BankQuestion { get; set; } = null!;
}