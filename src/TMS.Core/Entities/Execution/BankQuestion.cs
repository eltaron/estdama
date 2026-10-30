using TMS.Core.Entities.Common;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Execution;

public class BankQuestion : BaseEntity
{
    public string QuestionTextAr { get; set; } = string.Empty;
    public string QuestionTextEn { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public int Marks { get; set; } = 1;
    public string? Category { get; set; }
    public string? Difficulty { get; set; }
    public QuestionScope Scope { get; set; } = QuestionScope.Theoretical;

    public ICollection<BankQuestionOption> Options { get; set; } = new List<BankQuestionOption>();
}