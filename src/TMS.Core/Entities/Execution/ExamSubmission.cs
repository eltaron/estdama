using TMS.Core.Entities.Common;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;

namespace TMS.Core.Entities.Execution;

public class ExamSubmission : BaseEntity
{
    public int ExamId { get; set; }
    public int TraineeId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? SubmitTime { get; set; }
    public decimal TotalScore { get; set; }
    public bool IsPassed { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.InProgress;

    public Exam Exam { get; set; } = null!;
    public Trainee Trainee { get; set; } = null!;
    public ICollection<ExamAnswer> Answers { get; set; } = new List<ExamAnswer>();
}
