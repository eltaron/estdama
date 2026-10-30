using System.ComponentModel.DataAnnotations;

namespace TMS.Web.ViewModels.Courses;

public class ScheduleFormViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }

    [Required]
    [Display(Name = "ScheduleForm.StartDate")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "ScheduleForm.EndDate")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    [Display(Name = "ScheduleForm.StartTime")]
    public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0);

    [Required]
    [Display(Name = "ScheduleForm.EndTime")]
    public TimeSpan EndTime { get; set; } = new TimeSpan(16, 0, 0);

    [Display(Name = "ScheduleForm.RoomId")]
    public int? RoomId { get; set; }

    [Required]
    [Display(Name = "ScheduleForm.InstructorId")]
    public int InstructorId { get; set; }

    [Display(Name = "ScheduleForm.Notes")]
    public string? Notes { get; set; }
}