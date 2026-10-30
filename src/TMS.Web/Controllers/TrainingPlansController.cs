using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Web.ViewModels.TrainingPlans;

namespace TMS.Web.Controllers;

[Authorize(Roles = "Admin,Supervisor,DepartmentHead")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TrainingPlansController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public TrainingPlansController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(TrainingPlanCategory? category)
    {
        IQueryable<TrainingPlan> query = _unitOfWork.Repository<TrainingPlan>().Query()
            .Include(p => p.Items)
                .ThenInclude(i => i.Course);

        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);

        var plans = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        ViewBag.CategoryFilter = category;
        return View(plans);
    }

    public async Task<IActionResult> Report(TrainingPlanCategory? category, int? year, int? quarter, PlanStatus? status, string? search)
    {
        IQueryable<TrainingPlan> query = _unitOfWork.Repository<TrainingPlan>().Query()
            .Include(p => p.Items)
                .ThenInclude(i => i.Course)
            .Include(p => p.Items)
                .ThenInclude(i => i.TargetDepartment)
            .Include(p => p.CreatedBy);

        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);
        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);
        if (quarter.HasValue)
            query = query.Where(p => p.Quarter == quarter.Value);
        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.NameAr.Contains(search) || p.NameEn.Contains(search));

        var plans = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        var courseIds = plans.SelectMany(p => p.Items).Select(i => i.CourseId).Distinct().ToList();

        var schedules = new List<CourseSchedule>();
        if (courseIds.Count > 0)
        {
            schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
                .Where(s => courseIds.Contains(s.CourseId))
                .Include(s => s.Instructor)
                .ToListAsync();
        }

        var scheduleIds = schedules.Select(s => s.Id).Distinct().ToList();
        var registrations = new List<Registration>();
        if (scheduleIds.Count > 0)
        {
            registrations = await _unitOfWork.Repository<Registration>().Query()
                .Where(r => scheduleIds.Contains(r.CourseScheduleId))
                .ToListAsync();
        }

        var regCounts = registrations
            .GroupBy(r => r.CourseScheduleId)
            .ToDictionary(g => g.Key, g => g.Count());
        var schedulesByCourse = schedules
            .GroupBy(s => s.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var vm = new TrainingPlansReportViewModel
        {
            Plans = plans.Select(p =>
            {
                var items = new List<TrainingPlanReportItemRow>();
                foreach (var item in p.Items)
                {
                    var scheds = schedulesByCourse.GetValueOrDefault(item.CourseId) ?? new List<CourseSchedule>();
                    items.Add(new TrainingPlanReportItemRow
                    {
                        Course = item.Course,
                        TargetDepartmentName = item.TargetDepartment?.NameAr,
                        PlannedQuarter = item.PlannedQuarter,
                        Notes = item.Notes,
                        ScheduleCount = scheds.Count,
                        FirstScheduleStart = scheds.Count > 0 ? scheds.Min(s => s.StartDate) : null,
                        LastScheduleEnd = scheds.Count > 0 ? scheds.Max(s => s.EndDate) : null,
                        Instructors = string.Join(", ", scheds
                            .Select(s => $"{s.Instructor.FirstNameAr} {s.Instructor.LastNameAr}")
                            .Distinct()),
                        Registrations = scheds.Sum(s => regCounts.GetValueOrDefault(s.Id))
                    });
                }
                return new TrainingPlanReportRow { Plan = p, Items = items };
            }).ToList(),
            Years = await _unitOfWork.Repository<TrainingPlan>().Query()
                .Select(p => p.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync(),
            Category = category,
            Year = year,
            Quarter = quarter,
            Status = status,
            Search = search
        };

        return View(vm);
    }

    public IActionResult Create()
    {
        return View(new TrainingPlan());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingPlan model)
    {
        if (!ModelState.IsValid) return View(model);

        model.CreatedById = _userManager.GetUserId(User);
        model.Status = PlanStatus.Draft;

        await _unitOfWork.Repository<TrainingPlan>().AddAsync(model);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إنشاء الخطة";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var plan = await _unitOfWork.Repository<TrainingPlan>().Query()
            .Include(p => p.Items)
                .ThenInclude(i => i.Course)
            .Include(p => p.Items)
                .ThenInclude(i => i.TargetDepartment)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return NotFound();

        await PopulateItemDropdownsAsync();
        return View(plan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int planId, int courseId, int? targetDepartmentId, int? plannedQuarter, string? notes)
    {
        var plan = await _unitOfWork.Repository<TrainingPlan>().GetByIdAsync(planId);
        if (plan == null) return NotFound();

        var duplicate = await _unitOfWork.Repository<TrainingPlanItem>().FirstOrDefaultAsync(
            i => i.TrainingPlanId == planId && i.CourseId == courseId);

        if (duplicate != null)
        {
            TempData["Error"] = "هذه الدورة موجودة بالفعل في الخطة";
            return RedirectToAction(nameof(Details), new { id = planId });
        }

        var item = new TrainingPlanItem
        {
            TrainingPlanId = planId,
            CourseId = courseId,
            TargetDepartmentId = targetDepartmentId,
            PlannedQuarter = plannedQuarter,
            Notes = notes
        };

        await _unitOfWork.Repository<TrainingPlanItem>().AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إضافة الدورة للخطة";
        return RedirectToAction(nameof(Details), new { id = planId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int id, int planId)
    {
        var item = await _unitOfWork.Repository<TrainingPlanItem>().GetByIdAsync(id);
        if (item == null) return NotFound();

        _unitOfWork.Repository<TrainingPlanItem>().Delete(item);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف العنصر من الخطة";
        return RedirectToAction(nameof(Details), new { id = planId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, PlanStatus status)
    {
        var plan = await _unitOfWork.Repository<TrainingPlan>().GetByIdAsync(id);
        if (plan == null) return NotFound();

        plan.Status = status;
        plan.ApprovedById = status == PlanStatus.Approved || status == PlanStatus.Active
            ? _userManager.GetUserId(User)
            : plan.ApprovedById;

        _unitOfWork.Repository<TrainingPlan>().Update(plan);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"تم تحديث حالة الخطة إلى {status}";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var plan = await _unitOfWork.Repository<TrainingPlan>().Query()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null) return NotFound();

        foreach (var item in plan.Items.ToList())
            _unitOfWork.Repository<TrainingPlanItem>().Delete(item);

        _unitOfWork.Repository<TrainingPlan>().Delete(plan);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف الخطة";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateItemDropdownsAsync()
    {
        var courses = await _unitOfWork.Repository<Course>().Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.NameAr)
            .ToListAsync();
        ViewBag.Courses = new SelectList(courses.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.NameAr} ({c.Code})"
        }), "Value", "Text");

        var departments = await _unitOfWork.Repository<Department>().Query()
            .Where(d => d.IsActive)
            .OrderBy(d => d.NameAr)
            .ToListAsync();
        ViewBag.Departments = new SelectList(departments, "Id", "NameAr");
    }
}