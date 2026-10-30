using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Execution;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Web.ViewModels.Execution;

namespace TMS.Web.Controllers;

[Authorize(Roles = "Admin,Supervisor")]
public class QuestionBankController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public QuestionBankController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? category, string? search)
    {
        var query = _unitOfWork.Repository<BankQuestion>().Query()
            .Include(q => q.Options)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(q => q.Category == category);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(q => q.QuestionTextAr.Contains(search) || q.QuestionTextEn.Contains(search));

        var list = await query.OrderByDescending(q => q.Id).ToListAsync();
        ViewBag.Categories = await _unitOfWork.Repository<BankQuestion>().Query()
            .Where(q => q.Category != null)
            .Select(q => q.Category!)
            .Distinct()
            .ToListAsync();
        ViewBag.Search = search;
        ViewBag.Category = category;
        return View(list);
    }

    public IActionResult Create()
    {
        ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));
        var vm = new ExamQuestionViewModel
        {
            Options = new List<QuestionOptionViewModel>
            {
                new() { Order = 1 }, new() { Order = 2 }, new() { Order = 3 }, new() { Order = 4 }
            }
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamQuestionViewModel model)
    {
        ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));
        var opts = model.Options.Where(o => !string.IsNullOrWhiteSpace(o.OptionTextAr)).ToList();
        if (opts.Count < 2)
            ModelState.AddModelError(nameof(model.Options), "أضف خيارين على الأقل");
        else if (opts.Count(o => o.IsCorrect) != 1)
            ModelState.AddModelError(nameof(model.Options), "حدد إجابة صحيحة واحدة فقط");
        if (string.IsNullOrWhiteSpace(model.QuestionTextAr))
            ModelState.AddModelError(nameof(model.QuestionTextAr), "اكتب نص السؤال");
        if (!ModelState.IsValid)
            return View(model);

        var q = new BankQuestion
        {
            QuestionTextAr = model.QuestionTextAr,
            QuestionTextEn = model.QuestionTextEn ?? string.Empty,
            QuestionType = model.QuestionType,
            Marks = model.Marks > 0 ? model.Marks : 1,
            Category = model.ExamTitle
        };
        await _unitOfWork.Repository<BankQuestion>().AddAsync(q);
        await _unitOfWork.SaveChangesAsync();

        int order = 1;
        foreach (var o in opts)
        {
            await _unitOfWork.Repository<BankQuestionOption>().AddAsync(new BankQuestionOption
            {
                BankQuestionId = q.Id,
                OptionTextAr = o.OptionTextAr,
                OptionTextEn = o.OptionTextEn ?? string.Empty,
                IsCorrect = o.IsCorrect,
                Order = order++
            });
        }
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "تمت إضافة السؤال إلى بنك الأسئلة";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var q = await _unitOfWork.Repository<BankQuestion>().Query()
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (q == null) return NotFound();
        ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));
        var vm = new ExamQuestionViewModel
        {
            Id = q.Id,
            QuestionTextAr = q.QuestionTextAr,
            QuestionTextEn = q.QuestionTextEn,
            QuestionType = q.QuestionType,
            Marks = q.Marks,
            ExamTitle = q.Category ?? string.Empty,
            IsEdit = true,
            Options = q.Options.OrderBy(o => o.Order).Select(o => new QuestionOptionViewModel
            {
                Id = o.Id, OptionTextAr = o.OptionTextAr, OptionTextEn = o.OptionTextEn, IsCorrect = o.IsCorrect, Order = o.Order
            }).ToList()
        };
        return View(nameof(Create), vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExamQuestionViewModel model)
    {
        var q = await _unitOfWork.Repository<BankQuestion>().Query()
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == model.Id);
        if (q == null) return NotFound();
        var opts = model.Options.Where(o => !string.IsNullOrWhiteSpace(o.OptionTextAr)).ToList();
        if (opts.Count < 2 || opts.Count(o => o.IsCorrect) != 1)
        {
            ViewBag.QuestionTypes = new SelectList(Enum.GetValues(typeof(QuestionType)));
            ModelState.AddModelError(nameof(model.Options), "تحتاج خيارين على الأقل وإجابة صحيحة واحدة");
            return View(nameof(Create), model);
        }
        q.QuestionTextAr = model.QuestionTextAr;
        q.QuestionTextEn = model.QuestionTextEn ?? string.Empty;
        q.QuestionType = model.QuestionType;
        q.Marks = model.Marks > 0 ? model.Marks : 1;
        q.Category = model.ExamTitle;
        foreach (var old in q.Options.ToList())
            _unitOfWork.Repository<BankQuestionOption>().Delete(old);
        await _unitOfWork.SaveChangesAsync();
        int order = 1;
        foreach (var o in opts)
        {
            await _unitOfWork.Repository<BankQuestionOption>().AddAsync(new BankQuestionOption
            {
                BankQuestionId = q.Id, OptionTextAr = o.OptionTextAr, OptionTextEn = o.OptionTextEn ?? string.Empty, IsCorrect = o.IsCorrect, Order = order++
            });
        }
        _unitOfWork.Repository<BankQuestion>().Update(q);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "تم تحديث السؤال";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var q = await _unitOfWork.Repository<BankQuestion>().Query()
            .Include(x => x.Options)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (q == null) return NotFound();
        foreach (var o in q.Options.ToList())
            _unitOfWork.Repository<BankQuestionOption>().Delete(o);
        _unitOfWork.Repository<BankQuestion>().Delete(q);
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "تم حذف السؤال";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportToExam(int bankQuestionId, int examId)
    {
        var bank = await _unitOfWork.Repository<BankQuestion>().Query()
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == bankQuestionId);
        var exam = await _unitOfWork.Repository<Exam>().GetByIdAsync(examId);
        if (bank == null || exam == null) return NotFound();

        var lastOrder = await _unitOfWork.Repository<ExamQuestion>().Query()
            .Where(q => q.ExamId == examId).Select(q => (int?)q.Order).MaxAsync() ?? 0;

        var eq = new ExamQuestion
        {
            ExamId = examId,
            QuestionTextAr = bank.QuestionTextAr,
            QuestionTextEn = bank.QuestionTextEn,
            QuestionType = bank.QuestionType,
            Marks = bank.Marks,
            Order = lastOrder + 1
        };
        await _unitOfWork.Repository<ExamQuestion>().AddAsync(eq);
        await _unitOfWork.SaveChangesAsync();
        foreach (var o in bank.Options.OrderBy(x => x.Order))
        {
            await _unitOfWork.Repository<ExamQuestionOption>().AddAsync(new ExamQuestionOption
            {
                ExamQuestionId = eq.Id, OptionTextAr = o.OptionTextAr, OptionTextEn = o.OptionTextEn, IsCorrect = o.IsCorrect, Order = o.Order
            });
        }
        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "تم استيراد السؤال إلى الاختبار";
        return RedirectToAction("ManageQuestions", "Exams", new { id = examId });
    }
}