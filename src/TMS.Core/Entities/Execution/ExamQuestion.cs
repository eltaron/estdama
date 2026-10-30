using TMS.Core.Entities.Common;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Execution;

public class ExamQuestion : BaseEntity
{
    public int ExamId { get; set; }
    public string QuestionTextAr { get; set; } = string.Empty;
    public string QuestionTextEn { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public QuestionScope Scope { get; set; } = QuestionScope.Theoretical;
    public int Marks { get; set; }
    public int Order { get; set; }

    public Exam Exam { get; set; } = null!;
    public ICollection<ExamQuestionOption> Options { get; set; } = new List<ExamQuestionOption>();
}
