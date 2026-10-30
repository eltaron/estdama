using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Archive;
using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Execution;
using TMS.Core.Entities.Finance;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Resources;
using TMS.Core.Entities.Trainees;

namespace TMS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();
    public DbSet<CourseCompetency> CourseCompetencies => Set<CourseCompetency>();
    public DbSet<Competency> Competencies => Set<Competency>();
    public DbSet<CompetencyCategory> CompetencyCategories => Set<CompetencyCategory>();
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();
    public DbSet<TrainingPlanItem> TrainingPlanItems => Set<TrainingPlanItem>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<InstructorSpecialization> InstructorSpecializations => Set<InstructorSpecialization>();
    public DbSet<InstructorEvaluation> InstructorEvaluations => Set<InstructorEvaluation>();
    public DbSet<InstructorPayment> InstructorPayments => Set<InstructorPayment>();
    public DbSet<Trainee> Trainees => Set<Trainee>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<WaitingList> WaitingLists => Set<WaitingList>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<AbsenceExcuse> AbsenceExcuses => Set<AbsenceExcuse>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamQuestionOption> ExamQuestionOptions => Set<ExamQuestionOption>();
    public DbSet<ExamSubmission> ExamSubmissions => Set<ExamSubmission>();
    public DbSet<ExamAnswer> ExamAnswers => Set<ExamAnswer>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomBooking> RoomBookings => Set<RoomBooking>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentLoan> EquipmentLoans => Set<EquipmentLoan>();
    public DbSet<FinanceTransaction> FinanceTransactions => Set<FinanceTransaction>();
    public DbSet<BankQuestion> BankQuestions => Set<BankQuestion>();
    public DbSet<BankQuestionOption> BankQuestionOptions => Set<BankQuestionOption>();
    public DbSet<ArchiveDocument> ArchiveDocuments => Set<ArchiveDocument>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            var principal = relationship.PrincipalEntityType.ClrType;
            if (principal.Namespace != "Microsoft.AspNetCore.Identity")
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
