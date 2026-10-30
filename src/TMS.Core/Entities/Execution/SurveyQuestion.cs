using TMS.Core.Entities.Common;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Execution;

public class SurveyQuestion : BaseEntity
{
    public int SurveyId { get; set; }
    public string QuestionTextAr { get; set; } = string.Empty;
    public string QuestionTextEn { get; set; } = string.Empty;
    public SurveyQuestionType QuestionType { get; set; } = SurveyQuestionType.Rating1to5;
    public int Order { get; set; }

    public Survey Survey { get; set; } = null!;
}
