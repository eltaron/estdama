using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Execution;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Web.ViewModels.Execution;

namespace TMS.Web.Controllers;

[Authorize]
public class ExamsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ExamsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var exams = await _unitOfWork.Repository<Exam>().Query()
            .Include(e => e.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Include(e => e.Questions)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();

        return View(exams);
    }

    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> Create(int courseScheduleId)
    {
        ViewBag.CourseScheduleId = courseScheduleId;
        await PopulateScheduleListAsync(courseScheduleId);
        ViewBag.ExamTypes = new SelectList(Enum.GetValues(typeof(ExamType)));
        return View(new Exam());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Exam exam)
    {
        ModelState.Remove(nameof(Exam.CourseSchedule));
        if (exam.CourseScheduleId <= 0)
            ModelState.AddModelError(nameof(exam.CourseScheduleId), "اختر الجدول التدريبي");
        if (exam.EndTime <= exam.StartTime)
            ModelState.AddModelError(nameof(exam.EndTime), "وقت الانتهاء يجب أن يكون بعد وقت البدء");
        if (exam.PassingMarks > exam.TotalMarks)
            ModelState.AddModelError(nameof(exam.PassingMarks), "درجة النجاح لا يمكن أن تتجاوز الدرجة الكلية");

        if (!ModelState.IsValid)
        {
            ViewBag.CourseScheduleId = exam.CourseScheduleId;
            await PopulateScheduleListAsync(exam.CourseScheduleId);
            ViewBag.ExamTypes = new SelectList(Enum.GetValues(typeof(ExamType)));
            return View(exam);
        }

        await _unitOfWork.Repository<Exam>().AddAsync(exam);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إنشاء الاختبار بنجاح";
        return RedirectToAction(nameof(ManageQuestions), new { id = exam.Id });
    }

    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ManageQuestions(int id)
    {
        var exam = await _unitOfWork.Repository<Exam>().Query()
            .Include(e => e.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null) return NotFound();
        return View(exam);
    }

    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> AddQuestion(int examId, QuestionScope? scope)
    {
        var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
        if (exam == null) return NotFound();

        ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));
        var vm = new ExamQuestionViewModel
        {
            ExamId = examId,
            ExamTitle = exam.TitleAr,
            Scope = scope ?? QuestionScope.Theoretical,
            Options = BuildDefaultOptions(QuestionType.MultipleChoice)
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(ExamQuestionViewModel model)
    {
        var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(model.ExamId);
        if (exam == null) return NotFound();

        ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));

        var nonEmptyOptions = model.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.OptionTextAr))
            .Select(o => new { o.OptionTextAr, o.OptionTextEn, o.IsCorrect })
            .ToList();

        if (nonEmptyOptions.Count < 2)
        {
            ModelState.AddModelError(nameof(model.Options), "أضف خيارين على الأقل");
        }
        else if (nonEmptyOptions.Count(o => o.IsCorrect) != 1)
        {
            ModelState.AddModelError(nameof(model.Options), "حدد إجابة صحيحة واحدة فقط");
        }

        if (string.IsNullOrWhiteSpace(model.QuestionTextAr))
            ModelState.AddModelError(nameof(model.QuestionTextAr), "اكتب نص السؤال");

        if (!ModelState.IsValid)
        {
            model.ExamTitle = exam.TitleAr;
            model.Options = BuildDefaultOptions(model.QuestionType);
            return View(model);
        }

        var lastOrder = await _unitOfWork.Repository<ExamQuestion>().Query()
            .Where(q => q.ExamId == model.ExamId)
            .Select(q => (int?)q.Order)
            .MaxAsync() ?? 0;

        var question = new ExamQuestion
        {
            ExamId = model.ExamId,
            QuestionTextAr = model.QuestionTextAr,
            QuestionTextEn = model.QuestionTextEn ?? string.Empty,
            QuestionType = model.QuestionType,
            Scope = model.Scope,
            Marks = model.Marks > 0 ? model.Marks : 1,
            Order = lastOrder + 1
        };

        await _unitOfWork.Repository<ExamQuestion>().AddAsync(question);
        await _unitOfWork.SaveChangesAsync();

        var order = 1;
        foreach (var opt in nonEmptyOptions)
        {
            await _unitOfWork.Repository<ExamQuestionOption>().AddAsync(new ExamQuestionOption
            {
                ExamQuestionId = question.Id,
                OptionTextAr = opt.OptionTextAr,
                OptionTextEn = opt.OptionTextEn ?? string.Empty,
                IsCorrect = opt.IsCorrect,
                Order = order++
            });
        }
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إضافة السؤال";
        return RedirectToAction(nameof(ManageQuestions), new { id = model.ExamId });
    }

    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> EditQuestion(int id)
    {
        var question = await _unitOfWork.Repository<ExamQuestion>().Query()
            .Include(q => q.Options)
            .Include(q => q.Exam)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return NotFound();

        ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));

        var vm = new ExamQuestionViewModel
        {
            Id = question.Id,
            ExamId = question.ExamId,
            ExamTitle = question.Exam.TitleAr,
            QuestionTextAr = question.QuestionTextAr,
            QuestionTextEn = question.QuestionTextEn,
            QuestionType = question.QuestionType,
            Marks = question.Marks,
            Order = question.Order,
            IsEdit = true,
            Options = question.Options.OrderBy(o => o.Order).Select(o => new QuestionOptionViewModel
            {
                Id = o.Id,
                OptionTextAr = o.OptionTextAr,
                OptionTextEn = o.OptionTextEn,
                IsCorrect = o.IsCorrect,
                Order = o.Order
            }).ToList()
        };

        return View(nameof(AddQuestion), vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(ExamQuestionViewModel model)
    {
        var question = await _unitOfWork.Repository<ExamQuestion>().Query()
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == model.Id);

        if (question == null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.QuestionTextAr))
        {
            ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));
            ModelState.AddModelError(nameof(model.QuestionTextAr), "اكتب نص السؤال");
            return View(nameof(AddQuestion), model);
        }

        var nonEmptyOptions = model.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.OptionTextAr))
            .Select(o => new { o.OptionTextAr, o.OptionTextEn, o.IsCorrect })
            .ToList();

        if (nonEmptyOptions.Count < 2 || nonEmptyOptions.Count(o => o.IsCorrect) != 1)
        {
            ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));
            ModelState.AddModelError(nameof(model.Options), "تحتاج خيارين على الأقل وإجابة صحيحة واحدة");
            return View(nameof(AddQuestion), model);
        }

        question.QuestionTextAr = model.QuestionTextAr;
        question.QuestionTextEn = model.QuestionTextEn ?? string.Empty;
        question.QuestionType = model.QuestionType;
        question.Marks = model.Marks > 0 ? model.Marks : 1;
        question.Order = model.Order > 0 ? model.Order : question.Order;

        foreach (var old in question.Options.ToList())
            _unitOfWork.Repository<ExamQuestionOption>().Delete(old);
        await _unitOfWork.SaveChangesAsync();

        var order = 1;
        foreach (var opt in nonEmptyOptions)
        {
            await _unitOfWork.Repository<ExamQuestionOption>().AddAsync(new ExamQuestionOption
            {
                ExamQuestionId = question.Id,
                OptionTextAr = opt.OptionTextAr,
                OptionTextEn = opt.OptionTextEn ?? string.Empty,
                IsCorrect = opt.IsCorrect,
                Order = order++
            });
        }

        _unitOfWork.Repository<ExamQuestion>().Update(question);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم تحديث السؤال";
        return RedirectToAction(nameof(ManageQuestions), new { id = question.ExamId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _unitOfWork.Repository<ExamQuestion>().Query()
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null) return NotFound();

        var examId = question.ExamId;
        foreach (var opt in question.Options.ToList())
            _unitOfWork.Repository<ExamQuestionOption>().Delete(opt);
        _unitOfWork.Repository<ExamQuestion>().Delete(question);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف السؤال";
        return RedirectToAction(nameof(ManageQuestions), new { id = examId });
    }

    private List<QuestionOptionViewModel> BuildDefaultOptions(QuestionType type)
    {
        if (type == QuestionType.TrueFalse)
        {
            return new List<QuestionOptionViewModel>
            {
                new() { OptionTextAr = "صحيح", OptionTextEn = "True", IsCorrect = true, Order = 1 },
                new() { OptionTextAr = "خطأ", OptionTextEn = "False", IsCorrect = false, Order = 2 }
            };
        }

        return new List<QuestionOptionViewModel>
        {
            new() { Order = 1 },
            new() { Order = 2 },
            new() { Order = 3 },
            new() { Order = 4 }
        };
    }

    public async Task<IActionResult> Details(int id)
    {
        var exam = await _unitOfWork.Repository<Exam>().Query()
            .Include(e => e.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Include(e => e.Questions)
                .ThenInclude(q => q.Options)
            .Include(e => e.Submissions)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null) return NotFound();
        return View(exam);
    }

    private async Task PopulateScheduleListAsync(int? selected)
    {
        var schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Where(s => s.Status == ScheduleStatus.Active || s.Status == ScheduleStatus.Scheduled)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        ViewBag.Schedules = new SelectList(
            schedules.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Course.NameAr} | {s.StartDate:yyyy-MM-dd}"
            }),
            "Value",
            "Text",
            selected);
    }
}
