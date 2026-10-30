using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Courses;
using TMS.Core.Interfaces;

namespace TMS.Web.Controllers;

[Authorize(Roles = "Admin,Supervisor")]
public class CourseCategoriesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseCategoriesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _unitOfWork.Repository<CourseCategory>().Query()
            .Include(c => c.SubCategories)
            .Include(c => c.Courses)
            .Where(c => c.IsActive)
            .OrderBy(c => c.NameAr)
            .ToListAsync();

        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseCategory model)
    {
        if (string.IsNullOrWhiteSpace(model.NameAr) && string.IsNullOrWhiteSpace(model.NameEn))
        {
            TempData["Error"] = "أدخل اسم التصنيف";
            return RedirectToAction(nameof(Index));
        }

        await _unitOfWork.Repository<CourseCategory>().AddAsync(model);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إضافة التصنيف";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _unitOfWork.Repository<CourseCategory>().GetByIdAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CourseCategory category)
    {
        if (!ModelState.IsValid) return View(category);

        var existing = await _unitOfWork.Repository<CourseCategory>().GetByIdAsync(category.Id);
        if (existing == null) return NotFound();

        existing.NameAr = category.NameAr;
        existing.NameEn = category.NameEn;
        existing.ParentCategoryId = category.ParentCategoryId;

        _unitOfWork.Repository<CourseCategory>().Update(existing);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم تحديث التصنيف";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _unitOfWork.Repository<CourseCategory>().Query()
            .Include(c => c.Courses)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return NotFound();

        if (category.Courses.Any() || category.SubCategories.Any())
        {
            TempData["Error"] = "لا يمكن حذف تصنيف يحتوي على دورات أو تصنيفات فرعية";
            return RedirectToAction(nameof(Index));
        }

        _unitOfWork.Repository<CourseCategory>().Delete(category);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف التصنيف";
        return RedirectToAction(nameof(Index));
    }
}