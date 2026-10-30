using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Web.ViewModels.Instructors;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.InstructorsView)]
public class InstructorsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public InstructorsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? search, InstructorType? type)
    {
        var query = _unitOfWork.Repository<Instructor>().Query()
            .Where(i => i.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(i => i.FirstNameAr.Contains(search) || i.LastNameAr.Contains(search) || i.FirstNameEn.Contains(search));

        if (type.HasValue)
            query = query.Where(i => i.Type == type.Value);

        var instructors = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

        var vm = instructors.Select(i => new InstructorListViewModel
        {
            Id = i.Id,
            FullNameAr = $"{i.FirstNameAr} {i.LastNameAr}",
            FullNameEn = $"{i.FirstNameEn} {i.LastNameEn}",
            SpecializationAr = i.SpecializationAr,
            Type = i.Type,
            HourlyRate = i.HourlyRate,
            IsActive = i.IsActive
        }).ToList();

        ViewBag.TypeFilter = new SelectList(Enum.GetValues(typeof(InstructorType)));
        ViewBag.Search = search;
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var instructor = await _unitOfWork.Repository<Instructor>().Query()
            .Include(i => i.Evaluations)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (instructor == null) return NotFound();
        return View(instructor);
    }

    [Authorize(Policy = PermissionCatalog.InstructorsCreate)]
    public IActionResult Create()
    {
        ViewBag.TypeList = new SelectList(Enum.GetValues(typeof(InstructorType)));
        return View(new InstructorFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.InstructorsCreate)]
    public async Task<IActionResult> Create(InstructorFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TypeList = new SelectList(Enum.GetValues(typeof(InstructorType)));
            return View(model);
        }

        var instructor = new Instructor
        {
            FirstNameAr = model.FirstNameAr,
            LastNameAr = model.LastNameAr,
            FirstNameEn = model.FirstNameEn,
            LastNameEn = model.LastNameEn,
            NationalId = model.NationalId,
            Email = model.Email,
            Phone = model.Phone,
            SpecializationAr = model.SpecializationAr,
            SpecializationEn = model.SpecializationEn,
            Type = model.Type,
            HourlyRate = model.HourlyRate,
            BankName = model.BankName,
            IBAN = model.IBAN,
            AccountNumber = model.AccountNumber
        };

        await _unitOfWork.Repository<Instructor>().AddAsync(instructor);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إضافة المدرب بنجاح";
        return RedirectToAction(nameof(Details), new { id = instructor.Id });
    }

    [Authorize(Policy = PermissionCatalog.InstructorsEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var instructor = await _unitOfWork.Repository<Instructor>().GetByIdAsync(id);
        if (instructor == null) return NotFound();

        var model = new InstructorFormViewModel
        {
            Id = instructor.Id,
            FirstNameAr = instructor.FirstNameAr,
            LastNameAr = instructor.LastNameAr,
            FirstNameEn = instructor.FirstNameEn,
            LastNameEn = instructor.LastNameEn,
            NationalId = instructor.NationalId,
            Email = instructor.Email,
            Phone = instructor.Phone,
            SpecializationAr = instructor.SpecializationAr,
            SpecializationEn = instructor.SpecializationEn,
            Type = instructor.Type,
            HourlyRate = instructor.HourlyRate,
            BankName = instructor.BankName,
            IBAN = instructor.IBAN,
            AccountNumber = instructor.AccountNumber
        };

        ViewBag.TypeList = new SelectList(Enum.GetValues(typeof(InstructorType)));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.InstructorsEdit)]
    public async Task<IActionResult> Edit(InstructorFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TypeList = new SelectList(Enum.GetValues(typeof(InstructorType)));
            return View(model);
        }

        var instructor = await _unitOfWork.Repository<Instructor>().GetByIdAsync(model.Id);
        if (instructor == null) return NotFound();

        instructor.FirstNameAr = model.FirstNameAr;
        instructor.LastNameAr = model.LastNameAr;
        instructor.FirstNameEn = model.FirstNameEn;
        instructor.LastNameEn = model.LastNameEn;
        instructor.NationalId = model.NationalId;
        instructor.Email = model.Email;
        instructor.Phone = model.Phone;
        instructor.SpecializationAr = model.SpecializationAr;
        instructor.SpecializationEn = model.SpecializationEn;
        instructor.Type = model.Type;
        instructor.HourlyRate = model.HourlyRate;
        instructor.BankName = model.BankName;
        instructor.IBAN = model.IBAN;
        instructor.AccountNumber = model.AccountNumber;
        instructor.ModifiedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Instructor>().Update(instructor);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم تحديث بيانات المدرب بنجاح";
        return RedirectToAction(nameof(Details), new { id = instructor.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.InstructorsDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var instructor = await _unitOfWork.Repository<Instructor>().GetByIdAsync(id);
        if (instructor == null) return NotFound();

        instructor.IsActive = false;
        instructor.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Instructor>().Update(instructor);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف المدرب بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> CoursesTaught(int? instructorId)
    {
        var query = _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .AsQueryable();

        if (instructorId.HasValue)
            query = query.Where(s => s.InstructorId == instructorId.Value);

        var schedules = await query
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();

        var scheduleIds = schedules.Select(s => s.Id).ToList();
        var registrationCounts = await _unitOfWork.Repository<Registration>().Query()
            .Where(r => scheduleIds.Contains(r.CourseScheduleId))
            .GroupBy(r => r.CourseScheduleId)
            .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ScheduleId, x => x.Count);

        ViewBag.RegistrationCounts = registrationCounts;

        var instructors = await _unitOfWork.Repository<Instructor>().Query()
            .Where(i => i.IsActive)
            .OrderBy(i => i.FirstNameAr)
            .ToListAsync();

        ViewBag.Instructors = new SelectList(instructors, "Id", "FirstNameAr", instructorId);
        ViewBag.SelectedInstructorId = instructorId;

        return View(schedules);
    }
}