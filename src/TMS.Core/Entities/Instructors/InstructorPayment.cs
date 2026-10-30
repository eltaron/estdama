using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Instructors;

public class InstructorPayment : BaseEntity
{
    public int InstructorId { get; set; }
    public int CourseScheduleId { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaymentDate { get; set; }

    public Instructor Instructor { get; set; } = null!;
    public CourseSchedule CourseSchedule { get; set; } = null!;
}
