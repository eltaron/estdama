using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Resources;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Web.ViewModels.Courses;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.CoursesView)]
public class CoursesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<Shared> _t;

    public CoursesController(IUnitOfWork unitOfWork, IStringLocalizer<Shared> t)
    {
        _unitOfWork = unitOfWork;
        _t = t;
    }

    public async Task<IActionResult> Index(string? search, CourseStatus? status, int? categoryId)
    {
        var query = _unitOfWork.Repository<Course>().Query()
            .Include(c => c.Category)
            .Include(c => c.Schedules)
            .Where(c => c.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.NameAr.Contains(search) || c.NameEn.Contains(search) || c.Code.Contains(search));

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);

        var courses = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var vm = courses.Select(c => new CourseListViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.NameAr,
            CategoryName = c.Category?.NameAr,
            Hours = c.Hours,
            MaxTrainees = c.MaxTrainees,
            Status = c.Status,
            ScheduleCount = c.Schedules.Count
        }).ToList();

        ViewBag.Categories = new SelectList(await _unitOfWork.Repository<CourseCategory>().GetAllAsync(), "Id", "NameAr");
        ViewBag.StatusFilter = new SelectList(Enum.GetValues(typeof(CourseStatus)));
        ViewBag.Search = search;
        ViewBag.StatusFilterValue = status;
        ViewBag.CategoryFilter = categoryId;

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var course = await _unitOfWork.Repository<Course>().Query()
            .Include(c => c.Category)
            .Include(c => c.Schedules)
                .ThenInclude(s => s.Room)
            .Include(c => c.Schedules)
                .ThenInclude(s => s.Instructor)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        var scheduleIds = course.Schedules.Select(s => s.Id).ToList();
        var registrationCounts = await _unitOfWork.Repository<Registration>()
            .Query()
            .Where(r => scheduleIds.Contains(r.CourseScheduleId) && (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Confirmed))
            .GroupBy(r => r.CourseScheduleId)
            .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ScheduleId, g => g.Count);

        var courseViewModel = new CourseDetailsViewModel
        {
            Id = course.Id,
            Code = course.Code,
            NameAr = course.NameAr,
            NameEn = course.NameEn,
            DescriptionAr = course.DescriptionAr,
            ObjectivesAr = course.ObjectivesAr,
            TargetAudience = course.TargetAudience,
            Hours = course.Hours,
            MaxTrainees = course.MaxTrainees,
            MinTrainees = course.MinTrainees,
            Status = course.Status,
            FeePerTrainee = course.FeePerTrainee,
            CategoryName = course.Category?.NameAr,
            Schedules = course.Schedules.Select(s => new ScheduleListItem
            {
                Id = s.Id,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                RoomName = s.Room?.NameAr,
                InstructorName = $"{s.Instructor.FirstNameAr} {s.Instructor.LastNameAr}",
                Status = s.Status,
                RegisteredCount = registrationCounts.TryGetValue(s.Id, out var cnt) ? cnt : 0
            }).ToList()
        };

        return View(courseViewModel);
    }

    [Authorize(Policy = PermissionCatalog.CoursesCreate)]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new CourseFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesCreate)]
    public async Task<IActionResult> Create(CourseFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        var lastCourse = await _unitOfWork.Repository<Course>().Query()
            .OrderByDescending(c => c.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastCourse != null && lastCourse.Code.StartsWith("TMS-"))
        {
            var numPart = lastCourse.Code.Split('-').Last();
            int.TryParse(numPart, out nextNumber);
            nextNumber++;
        }

        var course = new Course
        {
            Code = $"TMS-{DateTime.Now.Year}-{nextNumber:D3}",
            NameAr = model.NameAr,
            NameEn = model.NameEn,
            DescriptionAr = model.DescriptionAr,
            DescriptionEn = model.DescriptionEn,
            ObjectivesAr = model.ObjectivesAr,
            ObjectivesEn = model.ObjectivesEn,
            TargetAudience = model.TargetAudience,
            Hours = model.Hours,
            MaxTrainees = model.MaxTrainees,
            MinTrainees = model.MinTrainees,
            CategoryId = model.CategoryId,
            FeePerTrainee = model.FeePerTrainee,
            Status = model.Status
        };

        await _unitOfWork.Repository<Course>().AddAsync(course);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إضافة الدورة بنجاح";
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }

    [Authorize(Policy = PermissionCatalog.CoursesEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(id);
        if (course == null) return NotFound();

        var model = new CourseFormViewModel
        {
            Id = course.Id,
            NameAr = course.NameAr,
            NameEn = course.NameEn,
            DescriptionAr = course.DescriptionAr,
            DescriptionEn = course.DescriptionEn,
            ObjectivesAr = course.ObjectivesAr,
            ObjectivesEn = course.ObjectivesEn,
            TargetAudience = course.TargetAudience,
            Hours = course.Hours,
            MaxTrainees = course.MaxTrainees,
            MinTrainees = course.MinTrainees,
            CategoryId = course.CategoryId,
            FeePerTrainee = course.FeePerTrainee,
            Status = course.Status
        };

        await PopulateDropdowns();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesEdit)]
    public async Task<IActionResult> Edit(CourseFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(model.Id);
        if (course == null) return NotFound();

        course.NameAr = model.NameAr;
        course.NameEn = model.NameEn;
        course.DescriptionAr = model.DescriptionAr;
        course.DescriptionEn = model.DescriptionEn;
        course.ObjectivesAr = model.ObjectivesAr;
        course.ObjectivesEn = model.ObjectivesEn;
        course.TargetAudience = model.TargetAudience;
        course.Hours = model.Hours;
        course.MaxTrainees = model.MaxTrainees;
        course.MinTrainees = model.MinTrainees;
        course.CategoryId = model.CategoryId;
        course.FeePerTrainee = model.FeePerTrainee;
        course.Status = model.Status;
        course.ModifiedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Course>().Update(course);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم تحديث الدورة بنجاح";
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(id);
        if (course == null) return NotFound();

        course.IsActive = false;
        course.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Course>().Update(course);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف الدورة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesEdit)]
    public async Task<IActionResult> Start(int id)
    {
        var course = await LoadWithSchedules(id);
        if (course == null) return NotFound();

        course.Status = CourseStatus.InProgress;
        foreach (var s in course.Schedules.Where(s => s.Status == ScheduleStatus.Scheduled))
            s.Status = ScheduleStatus.Active;

        _unitOfWork.Repository<Course>().Update(course);
        foreach (var s in course.Schedules.Where(s => s.Status == ScheduleStatus.Active))
            _unitOfWork.Repository<CourseSchedule>().Update(s);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم بدء الدورة بنجاح";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesEdit)]
    public async Task<IActionResult> Complete(int id)
    {
        var course = await LoadWithSchedules(id);
        if (course == null) return NotFound();

        course.Status = CourseStatus.Completed;
        foreach (var s in course.Schedules.Where(s => s.Status != ScheduleStatus.Cancelled))
            s.Status = ScheduleStatus.Completed;

        _unitOfWork.Repository<Course>().Update(course);
        foreach (var s in course.Schedules.Where(s => s.Status == ScheduleStatus.Completed))
            _unitOfWork.Repository<CourseSchedule>().Update(s);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إنهاء الدورة بنجاح";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesEdit)]
    public async Task<IActionResult> Cancel(int id)
    {
        var course = await LoadWithSchedules(id);
        if (course == null) return NotFound();

        course.Status = CourseStatus.Cancelled;
        foreach (var s in course.Schedules.Where(s => s.Status != ScheduleStatus.Completed))
            s.Status = ScheduleStatus.Cancelled;

        _unitOfWork.Repository<Course>().Update(course);
        foreach (var s in course.Schedules.Where(s => s.Status == ScheduleStatus.Cancelled))
            _unitOfWork.Repository<CourseSchedule>().Update(s);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إلغاء الدورة";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = PermissionCatalog.CoursesEdit)]
    public async Task<IActionResult> Postpone(int id)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(id);
        if (course == null) return NotFound();
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesEdit)]
    public async Task<IActionResult> Postpone(int id, int days)
    {
        if (days <= 0) days = 1;

        var course = await LoadWithSchedules(id);
        if (course == null) return NotFound();

        foreach (var s in course.Schedules.Where(s => s.Status != ScheduleStatus.Cancelled))
        {
            s.StartDate = s.StartDate.AddDays(days);
            s.EndDate = s.EndDate.AddDays(days);
            _unitOfWork.Repository<CourseSchedule>().Update(s);
        }

        if (course.Status == CourseStatus.Planned)
            course.Status = CourseStatus.OpenForRegistration;
        course.ModifiedAt = DateTime.UtcNow;
        _unitOfWork.Repository<Course>().Update(course);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"تم تأجيل الدورة {days} يوم";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<Course?> LoadWithSchedules(int id)
    {
        return await _unitOfWork.Repository<Course>().Query()
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    [Authorize(Policy = PermissionCatalog.CoursesCreate)]
    public async Task<IActionResult> CreateSchedule(int courseId)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(courseId);
        if (course == null) return NotFound();

        ViewBag.CourseName = course.NameAr;
        await PopulateScheduleDropdowns();
        return View(new ScheduleFormViewModel { CourseId = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesCreate)]
    public async Task<IActionResult> CreateSchedule(ScheduleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var course = await _unitOfWork.Repository<Course>().GetByIdAsync(model.CourseId);
            ViewBag.CourseName = course?.NameAr;
            await PopulateScheduleDropdowns();
            return View(model);
        }

        var otherSchedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Where(s => s.Status != ScheduleStatus.Cancelled)
            .ToListAsync();

        var instructorBusy = otherSchedules.Any(s =>
            s.InstructorId == model.InstructorId &&
            SchedulesOverlap(model.StartDate, model.EndDate, model.StartTime, model.EndTime,
                s.StartDate, s.EndDate, s.StartTime, s.EndTime));

        var roomBusy = model.RoomId.HasValue && otherSchedules.Any(s =>
            s.RoomId == model.RoomId &&
            SchedulesOverlap(model.StartDate, model.EndDate, model.StartTime, model.EndTime,
                s.StartDate, s.EndDate, s.StartTime, s.EndTime));

        if (instructorBusy || roomBusy)
        {
            var course = await _unitOfWork.Repository<Course>().GetByIdAsync(model.CourseId);
            ViewBag.CourseName = course?.NameAr;
            await PopulateScheduleDropdowns();
            ModelState.AddModelError(string.Empty, _t["Conflicts.SaveBlocked"].Value);
            return View(model);
        }

        var schedule = new CourseSchedule
        {
            CourseId = model.CourseId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            RoomId = model.RoomId,
            InstructorId = model.InstructorId,
            Notes = model.Notes,
            Status = ScheduleStatus.Scheduled
        };

        await _unitOfWork.Repository<CourseSchedule>().AddAsync(schedule);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إنشاء الجدول بنجاح";
        return RedirectToAction(nameof(Details), new { id = model.CourseId });
    }

    private static bool SchedulesOverlap(DateTime aStart, DateTime aEnd, TimeSpan aFrom, TimeSpan aTo,
        DateTime bStart, DateTime bEnd, TimeSpan bFrom, TimeSpan bTo) =>
        aStart <= bEnd && bStart <= aEnd && aFrom < bTo && bFrom < aTo;

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.CoursesDelete)]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var schedule = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null) return NotFound();

        var courseId = schedule.CourseId;
        var hasRegistrations = await _unitOfWork.Repository<Registration>().Query()
            .AnyAsync(r => r.CourseScheduleId == id);

        if (hasRegistrations)
        {
            TempData["Error"] = "لا يمكن حذف الجدول لوجود تسجيلات عليه";
            return RedirectToAction(nameof(Details), new { id = courseId });
        }

        _unitOfWork.Repository<CourseSchedule>().Delete(schedule);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف الجدول";
        return RedirectToAction(nameof(Details), new { id = courseId });
    }

    private async Task PopulateScheduleDropdowns()
    {
        var rooms = await _unitOfWork.Repository<Room>().Query()
            .Where(r => r.IsActive)
            .OrderBy(r => r.NameAr)
            .ToListAsync();
        ViewBag.Rooms = new SelectList(rooms, "Id", "NameAr");

        var instructors = await _unitOfWork.Repository<TMS.Core.Entities.Instructors.Instructor>().Query()
            .Where(i => i.IsActive)
            .OrderBy(i => i.FirstNameAr)
            .ToListAsync();
        ViewBag.Instructors = new SelectList(
            instructors.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = $"{i.FirstNameAr} {i.LastNameAr}"
            }),
            "Value",
            "Text");
    }

    private async Task PopulateDropdowns()
    {
        var categories = await _unitOfWork.Repository<CourseCategory>().GetAllAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "NameAr");
        ViewBag.StatusList = new SelectList(Enum.GetValues(typeof(CourseStatus)));
        ViewBag.AudienceList = new SelectList(Enum.GetValues(typeof(TargetAudience)));
    }
}