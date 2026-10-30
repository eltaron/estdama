using TMS.Core.Entities.Common;
using TMS.Core.Entities.Identity;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Instructors;

public class Instructor : AuditableEntity
{
    public string? UserId { get; set; }
    public string FirstNameAr { get; set; } = string.Empty;
    public string LastNameAr { get; set; } = string.Empty;
    public string FirstNameEn { get; set; } = string.Empty;
    public string LastNameEn { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? SpecializationAr { get; set; }
    public string? SpecializationEn { get; set; }
    public InstructorType Type { get; set; } = InstructorType.External;
    public string? CVFilePath { get; set; }
    public decimal HourlyRate { get; set; }
    public string? BankName { get; set; }
    public string? IBAN { get; set; }
    public string? AccountNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public ApplicationUser? User { get; set; }
    public ICollection<InstructorSpecialization> Specializations { get; set; } = new List<InstructorSpecialization>();
    public ICollection<InstructorEvaluation> Evaluations { get; set; } = new List<InstructorEvaluation>();
    public ICollection<InstructorPayment> Payments { get; set; } = new List<InstructorPayment>();
}
