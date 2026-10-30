using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Courses;

public class CompetencyCategory : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    public ICollection<Competency> Competencies { get; set; } = new List<Competency>();
}
