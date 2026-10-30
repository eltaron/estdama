using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Courses;

public class Competency : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public int? CategoryId { get; set; }

    public CompetencyCategory? Category { get; set; }
    public ICollection<CourseCompetency> CourseCompetencies { get; set; } = new List<CourseCompetency>();
}
