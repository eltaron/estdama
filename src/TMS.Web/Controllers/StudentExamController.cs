using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Execution;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Web.ViewModels.Execution;

namespace TMS.Web.Controllers;

[AllowAnonymous]
public class StudentExamController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentExamController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new StudentExamLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(StudentExamLoginViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.NationalId) || string.IsNullOrWhiteSpace(model.CourseCode))
        {
            ModelState.AddModelError(string.Empty, "أدخل الرقم القومي ورقم الدورة");
            return View(model);
        }

        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.User.NationalId == model.NationalId);

        if (trainee == null)
        {
            ModelState.AddModelError(string.Empty, "الرقم القومي غير موجود");
            return View(model);
        }

        var schedule = await _unitOfWork.Repository<TMS.Core.Entities.Courses.CourseSchedule>().Query()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Course.Code == model.CourseCode);

        if (schedule == null)
        {
            var course = await _unitOfWork.Repository<TMS.Core.Entities.Courses.Course>().Query()
                .FirstOrDefaultAsync(c => c.Code == model.CourseCode);
            if (course != null)
            {
                schedule = await _unitOfWork.Repository<TMS.Core.Entities.Courses.CourseSchedule>().Query()
                    .Include(s => s.Course)
                    .Where(s => s.CourseId == course.Id)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();
            }
        }

        if (schedule == null)
        {
            ModelState.AddModelError(string.Empty, "رقم الدورة غير صحيح");
            return View(model);
        }

        var isRegistered = await _unitOfWork.Repository<Registration>().Query()
            .AnyAsync(r => r.TraineeId == trainee.Id && r.CourseScheduleId == schedule.Id);

        if (!isRegistered)
        {
            ModelState.AddModelError(string.Empty, "الطالب غير مسجل في هذه الدورة");
            return View(model);
        }

        TempData["StudentTraineeId"] = trainee.Id;
        TempData["StudentScheduleId"] = schedule.Id;
        TempData.Keep();

        return RedirectToAction(nameof(Exams), new { traineeId = trainee.Id, scheduleId = schedule.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Exams(int traineeId, int scheduleId)
    {
        var exams = await _unitOfWork.Repository<Exam>().Query()
            .Include(e => e.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Include(e => e.Questions)
            .Where(e => e.CourseScheduleId == scheduleId)
            .ToListAsync();

        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == traineeId);

        if (trainee == null) return NotFound();

        ViewBag.TraineeId = traineeId;
        ViewBag.ScheduleId = scheduleId;
        ViewBag.TraineeName = trainee.User.FullNameAr;

        var submissions = await _unitOfWork.Repository<ExamSubmission>().Query()
            .Where(s => s.TraineeId == traineeId && exams.Select(e => e.Id).Contains(s.ExamId))
            .ToListAsync();
        ViewBag.Submissions = submissions.ToDictionary(s => s.ExamId);

        return View(exams);
    }

    [HttpGet]
    public async Task<IActionResult> Take(int examId, int traineeId, QuestionScope? scope)
    {
        var exam = await _unitOfWork.Repository<Exam>().Query()
            .Include(e => e.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == examId);

        if (exam == null) return NotFound();

        var existing = await _unitOfWork.Repository<ExamSubmission>().Query()
            .FirstOrDefaultAsync(s => s.ExamId == examId && s.TraineeId == traineeId && s.Status == SubmissionStatus.Completed);

        if (existing != null)
        {
            TempData["Error"] = "لقد أديت هذا الاختبار مسبقاً";
            return RedirectToAction(nameof(Result), new { examId, traineeId });
        }

        var questions = exam.Questions.AsQueryable();
        if (scope.HasValue)
            questions = questions.Where(q => q.Scope == scope.Value);

        var list = questions.OrderBy(q => q.Order).ToList();
        if (!list.Any())
        {
            TempData["Error"] = "لا توجد أسئلة في هذا القسم";
            return RedirectToAction(nameof(Exams), new { traineeId, scheduleId = exam.CourseScheduleId });
        }

        if (exam.ShuffleQuestions)
            list = list.OrderBy(_ => Guid.NewGuid()).ToList();

        var vm = new StudentExamTakeViewModel
        {
            ExamId = exam.Id,
            ExamTitle = exam.TitleAr,
            TraineeId = traineeId,
            DurationMinutes = exam.DurationMinutes,
            Questions = list.Select(q => new StudentQuestionViewModel
            {
                QuestionId = q.Id,
                QuestionTextAr = q.QuestionTextAr,
                QuestionType = q.QuestionType,
                Scope = q.Scope,
                Marks = q.Marks,
                Options = q.Options.OrderBy(o => o.Order).Select(o => new StudentOptionViewModel
                {
                    Id = o.Id,
                    OptionTextAr = o.OptionTextAr
                }).ToList()
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(StudentExamTakeViewModel model, Dictionary<int, int> answers)
    {
        var exam = await _unitOfWork.Repository<Exam>().Query()
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == model.ExamId);

        if (exam == null) return NotFound();

        var existing = await _unitOfWork.Repository<ExamSubmission>().Query()
            .FirstOrDefaultAsync(s => s.ExamId == model.ExamId && s.TraineeId == model.TraineeId && s.Status == SubmissionStatus.Completed);

        if (existing != null)
            return RedirectToAction(nameof(Result), new { examId = model.ExamId, traineeId = model.TraineeId });

        var questionDict = exam.Questions.ToDictionary(q => q.Id);
        decimal totalScore = 0;

        var submission = new ExamSubmission
        {
            ExamId = model.ExamId,
            TraineeId = model.TraineeId,
            StartTime = DateTime.UtcNow,
            SubmitTime = DateTime.UtcNow,
            Status = SubmissionStatus.Completed
        };

        await _unitOfWork.Repository<ExamSubmission>().AddAsync(submission);
        await _unitOfWork.SaveChangesAsync();

        foreach (var q in model.Questions)
        {
            if (!answers.TryGetValue(q.QuestionId, out var selectedId))
                continue;

            if (!questionDict.TryGetValue(q.QuestionId, out var question))
                continue;

            var option = question.Options.FirstOrDefault(o => o.Id == selectedId);
            var isCorrect = option?.IsCorrect ?? false;
            if (isCorrect) totalScore += question.Marks;

            await _unitOfWork.Repository<ExamAnswer>().AddAsync(new ExamAnswer
            {
                ExamSubmissionId = submission.Id,
                ExamQuestionId = q.QuestionId,
                SelectedOptionId = selectedId,
                IsCorrect = isCorrect
            });
        }

        submission.TotalScore = totalScore;
        submission.IsPassed = totalScore >= exam.PassingMarks;
        _unitOfWork.Repository<ExamSubmission>().Update(submission);
        await _unitOfWork.SaveChangesAsync();

        return RedirectToAction(nameof(Result), new { examId = model.ExamId, traineeId = model.TraineeId });
    }

    [HttpGet]
    public async Task<IActionResult> Result(int examId, int traineeId)
    {
        var submission = await _unitOfWork.Repository<ExamSubmission>().Query()
            .Include(s => s.Exam)
            .Include(s => s.Trainee)
                .ThenInclude(t => t.User)
            .Include(s => s.Answers)
            .Where(s => s.ExamId == examId && s.TraineeId == traineeId)
            .OrderByDescending(s => s.SubmitTime)
            .FirstOrDefaultAsync();

        if (submission == null) return NotFound();
        return View(submission);
    }
}