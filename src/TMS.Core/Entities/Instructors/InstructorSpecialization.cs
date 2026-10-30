using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Instructors;

public class InstructorSpecialization : BaseEntity
{
    public int InstructorId { get; set; }
    public string SpecializationAr { get; set; } = string.Empty;
    public string SpecializationEn { get; set; } = string.Empty;

    public Instructor Instructor { get; set; } = null!;
}
