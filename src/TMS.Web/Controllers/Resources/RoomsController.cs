using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Resources;
using TMS.Core.Interfaces;

namespace TMS.Web.Controllers;

[Authorize(Roles = "Admin,Supervisor")]
public class RoomsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public RoomsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var rooms = await _unitOfWork.Repository<Room>().Query()
            .Include(r => r.Bookings)
            .ToListAsync();

        return View(rooms);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Room room)
    {
        if (!ModelState.IsValid) return View(room);

        await _unitOfWork.Repository<Room>().AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إضافة القاعة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(id);
        if (room == null) return NotFound();
        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Room room)
    {
        if (!ModelState.IsValid) return View(room);

        _unitOfWork.Repository<Room>().Update(room);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم تحديث القاعة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _unitOfWork.Repository<Room>().GetByIdAsync(id);
        if (room == null) return NotFound();

        room.IsActive = false;
        _unitOfWork.Repository<Room>().Update(room);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف القاعة";
        return RedirectToAction(nameof(Index));
    }
}
