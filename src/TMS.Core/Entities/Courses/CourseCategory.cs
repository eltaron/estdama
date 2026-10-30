using TMS.Core.Entities.Common;

namespace TMS.Core.Entities.Courses;

public class CourseCategory : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public CourseCategory? ParentCategory { get; set; }
    public ICollection<CourseCategory> SubCategories { get; set; } = new List<CourseCategory>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
