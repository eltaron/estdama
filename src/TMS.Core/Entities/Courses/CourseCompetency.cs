using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Courses;

public class CourseCompetency : BaseEntity
{
    public int CourseId { get; set; }
    public int CompetencyId { get; set; }

    public Course Course { get; set; } = null!;
    public Competency Competency { get; set; } = null!;
}
