using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Execution;

public class ExamQuestionOption : BaseEntity
{
    public int ExamQuestionId { get; set; }
    public string OptionTextAr { get; set; } = string.Empty;
    public string OptionTextEn { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }

    public ExamQuestion ExamQuestion { get; set; } = null!;
}
