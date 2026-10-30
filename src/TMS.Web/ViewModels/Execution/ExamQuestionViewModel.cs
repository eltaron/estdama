using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Execution;

public class ExamQuestionViewModel
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
    public QuestionScope Scope { get; set; } = QuestionScope.Theoretical;
    public string QuestionTextAr { get; set; } = string.Empty;
    public string QuestionTextEn { get; set; } = string.Empty;
    public int Marks { get; set; } = 1;
    public int Order { get; set; } = 1;
    public bool IsEdit { get; set; }
    public List<QuestionOptionViewModel> Options { get; set; } = new();
}

public class QuestionOptionViewModel
{
    public int Id { get; set; }
    public string OptionTextAr { get; set; } = string.Empty;
    public string OptionTextEn { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}