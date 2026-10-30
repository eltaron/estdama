using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Execution;

public class SurveyAnswer : BaseEntity
{
    public int SurveyResponseId { get; set; }
    public int SurveyQuestionId { get; set; }
    public int? RatingValue { get; set; }
    public bool? BoolValue { get; set; }
    public string? TextValue { get; set; }

    public SurveyResponse SurveyResponse { get; set; } = null!;
    public SurveyQuestion SurveyQuestion { get; set; } = null!;
}
