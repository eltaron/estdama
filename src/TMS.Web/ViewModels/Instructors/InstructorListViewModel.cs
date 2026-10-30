using TMS.Core.Enums;

namespace TMS.Web.ViewModels.Instructors;

public class InstructorListViewModel
{
    public int Id { get; set; }
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string? SpecializationAr { get; set; }
    public InstructorType Type { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal AverageRating { get; set; }
    public bool IsActive { get; set; }
}
