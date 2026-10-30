using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Execution;

public class ExamAnswer : BaseEntity
{
    public int ExamSubmissionId { get; set; }
    public int ExamQuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }

    public ExamSubmission ExamSubmission { get; set; } = null!;
    public ExamQuestion ExamQuestion { get; set; } = null!;
    public ExamQuestionOption? SelectedOption { get; set; }
}
