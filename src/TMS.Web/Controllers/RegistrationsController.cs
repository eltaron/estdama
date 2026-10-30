using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Finance;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Web.ViewModels.Registrations;

namespace TMS.Web.Controllers;

[Authorize(Roles = "Admin,Supervisor")]
public class RegistrationsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public RegistrationsController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? courseScheduleId)
    {
        IQueryable<Registration> query = _unitOfWork.Repository<Registration>().Query()
            .Include(r => r.Trainee)
                .ThenInclude(t => t.User)
            .Include(r => r.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Include(r => r.CourseSchedule)
                .ThenInclude(s => s.Instructor);

        if (courseScheduleId.HasValue)
            query = query.Where(r => r.CourseScheduleId == courseScheduleId.Value);

        var registrations = await query.OrderByDescending(r => r.RegisteredAt).ToListAsync();

        await PopulateScheduleFilterAsync(courseScheduleId);
        ViewBag.SelectedSchedule = courseScheduleId;
        return View(registrations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Filter(int? courseScheduleId)
        => RedirectToAction(nameof(Index), new { courseScheduleId });

    public async Task<IActionResult> Create(int courseScheduleId)
    {
        var schedule = await LoadScheduleAsync(courseScheduleId);
        if (schedule == null) return NotFound();

        ViewBag.ScheduleName = $"{schedule.Course.NameAr} | {schedule.StartDate:yyyy-MM-dd}";
        await PopulateFormDropdownsAsync(schedule);
        return View(new RegistrationFormViewModel { CourseScheduleId = courseScheduleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrationFormViewModel model)
    {
        var schedule = await LoadScheduleAsync(model.CourseScheduleId);
        if (schedule == null) return NotFound();

        var exists = await _unitOfWork.Repository<Registration>().Query()
            .AnyAsync(r => r.CourseScheduleId == model.CourseScheduleId && r.TraineeId == model.TraineeId
                        && r.Status != RegistrationStatus.Dropped);

        if (exists)
        {
            TempData["Error"] = "هذا المتدرب مسجل بالفعل في هذا الجدول";
            ViewBag.ScheduleName = $"{schedule.Course.NameAr} | {schedule.StartDate:yyyy-MM-dd}";
            await PopulateFormDropdownsAsync(schedule);
            return View(model);
        }

        var status = model.Status;
        var activeCount = await _unitOfWork.Repository<Registration>().Query()
            .CountAsync(r => r.CourseScheduleId == model.CourseScheduleId
                          && (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Confirmed));

        if ((status == RegistrationStatus.Registered || status == RegistrationStatus.Confirmed)
            && schedule.Course.MaxTrainees > 0 && activeCount >= schedule.Course.MaxTrainees)
        {
            status = RegistrationStatus.Waitlisted;
            TempData["Warnings"] = $"اكتمل العدد الأقصى ({schedule.Course.MaxTrainees})، تم التحويل لقائمة الانتظار";
        }

        var trainee = await _unitOfWork.Repository<Trainee>().GetByIdAsync(model.TraineeId);
        var fee = model.FeeAmount ?? schedule.Course.FeePerTrainee;

        var registration = new Registration
        {
            CourseScheduleId = model.CourseScheduleId,
            TraineeId = model.TraineeId,
            Status = status,
            RegisteredAt = DateTime.UtcNow,
            RegisteredById = _userManager.GetUserId(User),
            Notes = model.Notes,
            FeeAmount = fee,
            AmountPaid = model.AmountPaid,
            PaymentStatus = model.PaymentStatus,
            PaidAt = model.AmountPaid > 0 ? DateTime.UtcNow : null
        };

        await _unitOfWork.Repository<Registration>().AddAsync(registration);
        await _unitOfWork.SaveChangesAsync();

        await SyncTraineeFeeTransactionAsync(registration);

        TempData["Success"] = $"تم تسجيل المتدرب {trainee?.User?.FullNameAr ?? ""} في الدورة";
        return RedirectToAction(nameof(Index), new { courseScheduleId = model.CourseScheduleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var registration = await _unitOfWork.Repository<Registration>().Query()
            .Include(r => r.CourseSchedule)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (registration == null) return NotFound();

        var activeCount = await _unitOfWork.Repository<Registration>().Query()
            .CountAsync(r => r.CourseScheduleId == registration.CourseScheduleId
                          && (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Confirmed));

        if ((registration.CourseSchedule.Course.MaxTrainees > 0)
            && activeCount >= registration.CourseSchedule.Course.MaxTrainees
            && registration.Status == RegistrationStatus.Waitlisted)
        {
            TempData["Error"] = "الجدول مكتمل العدد";
        }
        else
        {
            registration.Status = RegistrationStatus.Confirmed;
            _unitOfWork.Repository<Registration>().Update(registration);
            await _unitOfWork.SaveChangesAsync();
            TempData["Success"] = "تم تأكيد التسجيل";
        }

        return RedirectToAction(nameof(Index), new { courseScheduleId = registration.CourseScheduleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetFee(int id, decimal? feeAmount, decimal? amountPaid, PaymentStatus paymentStatus)
    {
        var registration = await _unitOfWork.Repository<Registration>().GetByIdAsync(id);
        if (registration == null) return NotFound();

        registration.FeeAmount = feeAmount;
        registration.AmountPaid = amountPaid;
        registration.PaymentStatus = paymentStatus;
        registration.PaidAt = paymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : null;
        _unitOfWork.Repository<Registration>().Update(registration);
        await _unitOfWork.SaveChangesAsync();

        await SyncTraineeFeeTransactionAsync(registration);

        TempData["Success"] = "تم تحديث الرسوم والمدفوعات";
        return RedirectToAction(nameof(Index), new { courseScheduleId = registration.CourseScheduleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var registration = await _unitOfWork.Repository<Registration>().GetByIdAsync(id);
        if (registration == null) return NotFound();

        registration.Status = RegistrationStatus.Dropped;
        _unitOfWork.Repository<Registration>().Update(registration);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم إلغاء/إسقاط التسجيل";
        return RedirectToAction(nameof(Index), new { courseScheduleId = registration.CourseScheduleId });
    }

    private async Task SyncTraineeFeeTransactionAsync(Registration registration)
    {
        if (registration.FeeAmount is null or <= 0) return;

        var existing = await _unitOfWork.Repository<FinanceTransaction>().Query()
            .FirstOrDefaultAsync(t => t.Type == FinanceTransactionType.TraineeFee
                                   && t.ReferenceNo == $"REG-{registration.Id}");

        if (existing != null)
        {
            existing.Amount = registration.FeeAmount.Value;
            existing.TransactionDate = DateTime.UtcNow;
            existing.DescriptionAr = $"رسوم تسجيل متدرب #{registration.Id}";
            _unitOfWork.Repository<FinanceTransaction>().Update(existing);
        }
        else
        {
            await _unitOfWork.Repository<FinanceTransaction>().AddAsync(new FinanceTransaction
            {
                CourseId = registration.CourseSchedule.CourseId,
                Type = FinanceTransactionType.TraineeFee,
                Amount = registration.FeeAmount.Value,
                TransactionDate = DateTime.UtcNow,
                DescriptionAr = $"رسوم تسجيل متدرب #{registration.Id}",
                ReferenceNo = $"REG-{registration.Id}",
                CreatedById = _userManager.GetUserId(User)
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<CourseSchedule?> LoadScheduleAsync(int id)
        => await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);

    private async Task PopulateScheduleFilterAsync(int? selected)
    {
        var schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
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

    private async Task PopulateFormDropdownsAsync(CourseSchedule schedule)
    {
        var trainees = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .Where(t => t.IsActive)
            .OrderBy(t => t.User.FullNameAr)
            .ToListAsync();

        ViewBag.Trainees = new SelectList(
            trainees.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.User.FullNameAr} ({(string.IsNullOrEmpty(t.EmployeeNumber) ? "خاص" : t.EmployeeNumber)})"
            }),
            "Value",
            "Text");
        ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(RegistrationStatus)));
        ViewBag.PaymentStatuses = SelectListForPaymentStatus();
        ViewBag.DefaultFee = schedule.Course.FeePerTrainee;
    }

    private SelectList SelectListForPaymentStatus()
        => new SelectList(
            new[]
            {
                new SelectListItem { Value = "3", Text = "غير مسدد" },
                new SelectListItem { Value = "4", Text = "مسدد جزئياً" },
                new SelectListItem { Value = "2", Text = "مسدد بالكامل" }
            },
            "Value",
            "Text");
}