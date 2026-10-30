namespace TMS.Web.ViewModels.Execution;

public class StudentExamLoginViewModel
{
    public string NationalId { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
}

public class StudentExamTakeViewModel
{
    public int ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public int TraineeId { get; set; }
    public string TraineeName { get; set; } = string.Empty;
    public List<StudentQuestionViewModel> Questions { get; set; } = new();
    public int DurationMinutes { get; set; }
}

public class StudentQuestionViewModel
{
    public int QuestionId { get; set; }
    public string QuestionTextAr { get; set; } = string.Empty;
    public TMS.Core.Enums.QuestionType QuestionType { get; set; }
    public TMS.Core.Enums.QuestionScope Scope { get; set; }
    public int Marks { get; set; }
    public List<StudentOptionViewModel> Options { get; set; } = new();
    public int? SelectedOptionId { get; set; }
}

public class StudentOptionViewModel
{
    public int Id { get; set; }
    public string OptionTextAr { get; set; } = string.Empty;
}