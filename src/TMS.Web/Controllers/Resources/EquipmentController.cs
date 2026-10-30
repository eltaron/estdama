using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Resources;
using TMS.Core.Enums;
using TMS.Core.Interfaces;

namespace TMS.Web.Controllers;

[Authorize(Roles = "Admin,Supervisor")]
public class EquipmentController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public EquipmentController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var equipment = await _unitOfWork.Repository<Equipment>().Query().ToListAsync();
        ViewBag.CategoryList = new SelectList(Enum.GetValues(typeof(EquipmentCategory)));
        return View(equipment);
    }

    public IActionResult Create()
    {
        ViewBag.CategoryList = new SelectList(Enum.GetValues(typeof(EquipmentCategory)));
        return View(new Equipment());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Equipment equipment)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CategoryList = new SelectList(Enum.GetValues(typeof(EquipmentCategory)));
            return View(equipment);
        }

        await _unitOfWork.Repository<Equipment>().AddAsync(equipment);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إضافة المعدة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var equipment = await _unitOfWork.Repository<Equipment>().Query()
            .Include(e => e.Loans)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (equipment == null) return NotFound();

        if (equipment.Loans.Any(l => l.Status == LoanStatus.Loaned))
        {
            TempData["Error"] = "لا يمكن حذف المعدة لوجود إعارات نشطة";
            return RedirectToAction(nameof(Index));
        }

        _unitOfWork.Repository<Equipment>().Delete(equipment);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف المعدة";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var equipment = await _unitOfWork.Repository<Equipment>().GetByIdAsync(id);
        if (equipment == null) return NotFound();

        equipment.IsAvailable = !equipment.IsAvailable;
        _unitOfWork.Repository<Equipment>().Update(equipment);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = equipment.IsAvailable ? "تم تفعيل توفر المعدة" : "تم تعطيل توفر المعدة";
        return RedirectToAction(nameof(Index));
    }
}
