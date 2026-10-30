using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Execution;
using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Web.ViewModels.Surveys;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.TrainingView)]
public class SurveysController : Controller
{
    private const int InstructorQuestionOrder = 1;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<Shared> _t;

    public SurveysController(IUnitOfWork unitOfWork, IStringLocalizer<Shared> t)
    {
        _unitOfWork = unitOfWork;
        _t = t;
    }

    public async Task<IActionResult> Index()
    {
        var schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .OrderByDescending(s => s.EndDate)
            .ToListAsync();

        var scheduleIds = schedules.Select(s => s.Id).ToList();

        var responses = await _unitOfWork.Repository<SurveyResponse>().Query()
            .Where(r => scheduleIds.Contains(r.CourseScheduleId))
            .GroupBy(r => r.CourseScheduleId)
            .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
            .ToListAsync();

        var responseCounts = responses.ToDictionary(r => r.ScheduleId, r => r.Count);

        var avgRatings = await InstructorAvgByScheduleAsync(scheduleIds);

        var items = schedules.Select(s => new SurveyListItemViewModel
        {
            ScheduleId = s.Id,
            CourseName = s.Course.NameAr,
            InstructorName = InstructorFullName(s.Instructor),
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            ResponseCount = responseCounts.GetValueOrDefault(s.Id),
            AverageRating = avgRatings.GetValueOrDefault(s.Id)
        }).ToList();

        return View(items);
    }

    public async Task<IActionResult> Fill(int id)
    {
        var schedule = await LoadScheduleAsync(id);
        if (schedule == null) return NotFound();

        var survey = await EnsureSurveyAsync(id);

        var registrations = await _unitOfWork.Repository<Registration>().Query()
            .Include(r => r.Trainee)
                .ThenInclude(t => t.User)
            .Where(r => r.CourseScheduleId == id &&
                (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Confirmed))
            .OrderBy(r => r.Trainee.User.FullNameAr)
            .ToListAsync();

        var existingAnswers = await _unitOfWork.Repository<SurveyAnswer>().Query()
            .Where(a => a.SurveyResponse.CourseScheduleId == id && a.SurveyResponse.SurveyId == survey.Id)
            .Include(a => a.SurveyResponse)
            .ToListAsync();

        var answersByTrainee = existingAnswers
            .GroupBy(a => a.SurveyResponse.TraineeId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(a => a.SurveyQuestion.Order, a => a));

        var vm = new SurveyFillViewModel
        {
            CourseScheduleId = schedule.Id,
            CourseName = schedule.Course.NameAr,
            InstructorName = InstructorFullName(schedule.Instructor),
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            Rows = registrations.Select(r =>
            {
                answersByTrainee.TryGetValue(r.TraineeId, out var byOrder);
                return new SurveyFillRow
                {
                    TraineeId = r.TraineeId,
                    TraineeName = r.Trainee.User.FullNameAr,
                    InstructorRating = byOrder != null && byOrder.ContainsKey(1) ? byOrder[1].RatingValue : null,
                    ContentRating = byOrder != null && byOrder.ContainsKey(2) ? byOrder[2].RatingValue : null,
                    FacilitiesRating = byOrder != null && byOrder.ContainsKey(3) ? byOrder[3].RatingValue : null,
                    Notes = byOrder != null && byOrder.ContainsKey(4) ? byOrder[4].TextValue : null,
                    HasExisting = byOrder != null
                };
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fill(SurveyFillViewModel vm)
    {
        if (vm.Rows == null || vm.Rows.Count == 0)
        {
            TempData["Error"] = _t["Surveys.NoTrainees"].Value;
            return RedirectToAction(nameof(Index));
        }

        var survey = await EnsureSurveyAsync(vm.CourseScheduleId);
        var questions = await _unitOfWork.Repository<SurveyQuestion>().Query()
            .Where(q => q.SurveyId == survey.Id)
            .OrderBy(q => q.Order)
            .ToListAsync();

        var existingResponses = await _unitOfWork.Repository<SurveyResponse>().Query()
            .Where(r => r.SurveyId == survey.Id && r.CourseScheduleId == vm.CourseScheduleId)
            .ToListAsync();
        var existingByTrainee = existingResponses.ToDictionary(r => r.TraineeId);

        var saved = 0;
        foreach (var row in vm.Rows.Where(r => r.TraineeId > 0))
        {
            var hasRating = row.InstructorRating.HasValue
                || row.ContentRating.HasValue
                || row.FacilitiesRating.HasValue;
            var hasText = !string.IsNullOrWhiteSpace(row.Notes);
            if (!hasRating && !hasText) continue;

            if (existingByTrainee.TryGetValue(row.TraineeId, out var response))
            {
                response.IsAnonymous = vm.IsAnonymous;
                _unitOfWork.Repository<SurveyResponse>().Update(response);
            }
            else
            {
                response = new SurveyResponse
                {
                    SurveyId = survey.Id,
                    TraineeId = row.TraineeId,
                    CourseScheduleId = vm.CourseScheduleId,
                    SubmittedAt = DateTime.UtcNow,
                    IsAnonymous = vm.IsAnonymous
                };
                await _unitOfWork.Repository<SurveyResponse>().AddAsync(response);
            }

            var answers = await _unitOfWork.Repository<SurveyAnswer>().Query()
                .Include(a => a.SurveyQuestion)
                .Where(a => a.SurveyResponseId == response.Id)
                .ToListAsync();
            var answersByOrder = answers.ToDictionary(a => a.SurveyQuestion.Order);

            var values = new Dictionary<int, int?>
            {
                [1] = row.InstructorRating,
                [2] = row.ContentRating,
                [3] = row.FacilitiesRating
            };
            foreach (var kv in values)
            {
                if (!kv.Value.HasValue) continue;
                if (answersByOrder.TryGetValue(kv.Key, out var answer))
                {
                    answer.RatingValue = kv.Value;
                    _unitOfWork.Repository<SurveyAnswer>().Update(answer);
                }
                else
                {
                    await _unitOfWork.Repository<SurveyAnswer>().AddAsync(new SurveyAnswer
                    {
                        SurveyResponseId = response.Id,
                        SurveyQuestionId = questions.First(q => q.Order == kv.Key).Id,
                        RatingValue = kv.Value
                    });
                }
            }

            if (hasText)
            {
                var notes = answersByOrder.TryGetValue(4, out var textAnswer)
                    ? textAnswer
                    : null;
                if (notes != null)
                {
                    notes.TextValue = row.Notes;
                    _unitOfWork.Repository<SurveyAnswer>().Update(notes);
                }
                else if (questions.Any(q => q.Order == 4))
                {
                    await _unitOfWork.Repository<SurveyAnswer>().AddAsync(new SurveyAnswer
                    {
                        SurveyResponseId = response.Id,
                        SurveyQuestionId = questions.First(q => q.Order == 4).Id,
                        TextValue = row.Notes
                    });
                }
            }

            saved++;
            existingByTrainee[row.TraineeId] = response;
        }

        await UpdateInstructorEvaluationAsync(vm.CourseScheduleId, survey);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = string.Format(_t["Surveys.SavedSuccess"].Value, saved);
        return RedirectToAction(nameof(Results), new { id = vm.CourseScheduleId });
    }

    public async Task<IActionResult> Results(int id)
    {
        var schedule = await LoadScheduleAsync(id);
        if (schedule == null) return NotFound();

        var survey = await EnsureSurveyAsync(id);

        var questions = await _unitOfWork.Repository<SurveyQuestion>().Query()
            .Where(q => q.SurveyId == survey.Id)
            .OrderBy(q => q.Order)
            .ToListAsync();

        var responses = await _unitOfWork.Repository<SurveyResponse>().Query()
            .Where(r => r.SurveyId == survey.Id && r.CourseScheduleId == id)
            .Include(r => r.Answers)
                .ThenInclude(a => a.SurveyQuestion)
            .Include(r => r.Trainee)
                .ThenInclude(t => t.User)
            .ToListAsync();

        var ratingQuestions = questions.Where(q => q.QuestionType == SurveyQuestionType.Rating1to5).ToList();
        var results = ratingQuestions.Select(q =>
        {
            var avg = responses
                .SelectMany(r => r.Answers.Where(a => a.SurveyQuestionId == q.Id && a.RatingValue.HasValue))
                .Select(a => (double)a.RatingValue!.Value)
                .ToList();
            return new SurveyQuestionResult
            {
                Title = QuestionTitle(q),
                Average = avg.Count > 0 ? Math.Round(avg.Average(), 2) : null
            };
        }).ToList();

        var comments = responses
            .SelectMany(r => r.Answers.Where(a => a.SurveyQuestion.Order == 4 && !string.IsNullOrWhiteSpace(a.TextValue)))
            .Select(a => new SurveyCommentItem
            {
                TraineeName = a.SurveyResponse.IsAnonymous ? "" : a.SurveyResponse.Trainee.User.FullNameAr,
                Anonymous = a.SurveyResponse.IsAnonymous,
                Text = a.TextValue
            })
            .ToList();

        var instructorAvg = await InstructorAvgByScheduleAsync(new List<int> { id });

        var evaluation = await _unitOfWork.Repository<InstructorEvaluation>().Query()
            .FirstOrDefaultAsync(e => e.CourseScheduleId == id);

        var vm = new SurveyResultsViewModel
        {
            CourseScheduleId = schedule.Id,
            CourseName = schedule.Course.NameAr,
            InstructorName = InstructorFullName(schedule.Instructor),
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            ResponseCount = responses.Count,
            Questions = results,
            Comments = comments,
            InstructorOverallAverage = instructorAvg.GetValueOrDefault(id),
            InstructorEvaluationCount = evaluation?.EvaluationCount ?? responses.Count
        };

        return View(vm);
    }

    private async Task<CourseSchedule?> LoadScheduleAsync(int id)
    {
        return await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    private async Task<Survey> EnsureSurveyAsync(int scheduleId)
    {
        var existing = await _unitOfWork.Repository<Survey>().Query()
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.CourseScheduleId == scheduleId);
        if (existing != null) return existing;

        var survey = new Survey
        {
            CourseScheduleId = scheduleId,
            TitleAr = "استطلاع رضا - تقييم الدورة",
            TitleEn = "Satisfaction Survey - Course Evaluation",
            IsActive = true,
            Questions = new List<SurveyQuestion>
            {
                new SurveyQuestion { Order = 1, QuestionType = SurveyQuestionType.Rating1to5, QuestionTextAr = "تقييم المدرب", QuestionTextEn = "Instructor Evaluation" },
                new SurveyQuestion { Order = 2, QuestionType = SurveyQuestionType.Rating1to5, QuestionTextAr = "تقييم المحتوى العلمي", QuestionTextEn = "Course Content" },
                new SurveyQuestion { Order = 3, QuestionType = SurveyQuestionType.Rating1to5, QuestionTextAr = "جودة القاعة والتجهيزات", QuestionTextEn = "Facilities & Equipment" },
                new SurveyQuestion { Order = 4, QuestionType = SurveyQuestionType.Text, QuestionTextAr = "ملاحظات عامة", QuestionTextEn = "General Comments" }
            }
        };

        await _unitOfWork.Repository<Survey>().AddAsync(survey);
        await _unitOfWork.SaveChangesAsync();

        return survey;
    }

    private async Task UpdateInstructorEvaluationAsync(int scheduleId, Survey survey)
    {
        var schedule = await _unitOfWork.Repository<CourseSchedule>().GetByIdAsync(scheduleId);
        if (schedule == null) return;

        var responses = await _unitOfWork.Repository<SurveyResponse>().Query()
            .Where(r => r.SurveyId == survey.Id && r.CourseScheduleId == scheduleId)
            .Include(r => r.Answers)
                .ThenInclude(a => a.SurveyQuestion)
            .ToListAsync();

        var ratings = responses
            .SelectMany(r => r.Answers.Where(a => a.SurveyQuestion.Order == InstructorQuestionOrder && a.RatingValue.HasValue))
            .Select(a => (double)a.RatingValue!.Value)
            .ToList();

        var evaluation = await _unitOfWork.Repository<InstructorEvaluation>().Query()
            .FirstOrDefaultAsync(e => e.InstructorId == schedule.InstructorId && e.CourseScheduleId == scheduleId);

        if (evaluation == null)
        {
            evaluation = new InstructorEvaluation
            {
                InstructorId = schedule.InstructorId,
                CourseScheduleId = scheduleId,
                AverageRating = ratings.Count > 0 ? (decimal)Math.Round(ratings.Average(), 2) : 0,
                EvaluationCount = responses.Count,
                CalculatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<InstructorEvaluation>().AddAsync(evaluation);
        }
        else
        {
            evaluation.AverageRating = ratings.Count > 0 ? (decimal)Math.Round(ratings.Average(), 2) : 0;
            evaluation.EvaluationCount = responses.Count;
            evaluation.CalculatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<InstructorEvaluation>().Update(evaluation);
        }
    }

    private async Task<Dictionary<int, double?>> InstructorAvgByScheduleAsync(List<int> scheduleIds)
    {
        if (scheduleIds.Count == 0) return new Dictionary<int, double?>();

        var rows = await _unitOfWork.Repository<SurveyAnswer>().Query()
            .Where(a => a.SurveyQuestion.Order == InstructorQuestionOrder
                && a.RatingValue.HasValue
                && scheduleIds.Contains(a.SurveyResponse.CourseScheduleId))
            .GroupBy(a => a.SurveyResponse.CourseScheduleId)
            .Select(g => new { ScheduleId = g.Key, Avg = g.Average(a => (double)a.RatingValue!.Value) })
            .ToListAsync();

        return rows.ToDictionary(r => r.ScheduleId, r => (double?)Math.Round(r.Avg, 2));
    }

    private static string InstructorFullName(Instructor instructor)
    {
        var en = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
        return en
            ? $"{instructor.FirstNameEn} {instructor.LastNameEn}".Trim()
            : $"{instructor.FirstNameAr} {instructor.LastNameAr}".Trim();
    }

    private static string QuestionTitle(SurveyQuestion q) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? q.QuestionTextEn : q.QuestionTextAr;
}