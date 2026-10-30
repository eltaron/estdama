namespace TMS.Web.ViewModels.Surveys;

public class SurveyListItemViewModel
{
    public int ScheduleId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ResponseCount { get; set; }
    public double? AverageRating { get; set; }
}

public class SurveyFillViewModel
{
    public int CourseScheduleId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAnonymous { get; set; } = true;
    public List<SurveyFillRow> Rows { get; set; } = new();
}

public class SurveyFillRow
{
    public int TraineeId { get; set; }
    public string TraineeName { get; set; } = string.Empty;
    public int? InstructorRating { get; set; }
    public int? ContentRating { get; set; }
    public int? FacilitiesRating { get; set; }
    public string? Notes { get; set; }
    public bool HasExisting { get; set; }
}

public class SurveyQuestionResult
{
    public string Title { get; set; } = string.Empty;
    public double? Average { get; set; }
}

public class SurveyCommentItem
{
    public string TraineeName { get; set; } = string.Empty;
    public bool Anonymous { get; set; }
    public string? Text { get; set; }
}

public class SurveyResultsViewModel
{
    public int CourseScheduleId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ResponseCount { get; set; }
    public List<SurveyQuestionResult> Questions { get; set; } = new();
    public List<SurveyCommentItem> Comments { get; set; } = new();
    public double? InstructorOverallAverage { get; set; }
    public int InstructorEvaluationCount { get; set; }
}