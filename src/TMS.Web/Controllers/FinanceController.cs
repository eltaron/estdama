using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Finance;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Web.ViewModels.Finance;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.AccountsView)]
public class FinanceController : Controller
{
    private const decimal MinistryDeductionRate = 0.15m;

    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public FinanceController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var current = DateTime.Today;
        year ??= current.Year;
        month ??= current.Month;

        var viewModel = new PayrollViewModel { Year = year.Value, Month = month.Value };

        var instructors = await _unitOfWork.Repository<Instructor>().Query()
            .Where(i => i.IsActive)
            .OrderBy(i => i.FirstNameAr)
            .ToListAsync();

        var schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Where(s => s.EndDate.Year == year.Value && s.EndDate.Month == month.Value
                     && s.Status == ScheduleStatus.Completed)
            .ToListAsync();

        foreach (var instructor in instructors)
        {
            var instructorSchedules = schedules
                .Where(s => s.InstructorId == instructor.Id)
                .ToList();

            if (!instructorSchedules.Any()) continue;

            var totalHours = instructorSchedules.Sum(s => (decimal)(s.EndDate - s.StartDate).TotalDays * (decimal)(s.EndTime - s.StartTime).TotalHours);

            viewModel.Rows.Add(new InstructorPayrollRow
            {
                Instructor = instructor,
                ScheduleCount = instructorSchedules.Count,
                TotalHours = totalHours,
                GrossAmount = totalHours * instructor.HourlyRate
            });
        }

        viewModel.TotalHours = viewModel.Rows.Sum(r => r.TotalHours);
        viewModel.TotalGross = viewModel.Rows.Sum(r => r.GrossAmount);

        await PopulateCourseCardsAsync(viewModel);

        return View(viewModel);
    }

    private async Task PopulateCourseCardsAsync(PayrollViewModel viewModel)
    {
        var courses = await _unitOfWork.Repository<Course>().Query()
            .Include(c => c.Schedules)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Take(50)
            .ToListAsync();

        var allScheduleIds = courses.SelectMany(c => c.Schedules.Select(s => s.Id)).ToList();
        var counts = allScheduleIds.Any()
            ? await _unitOfWork.Repository<Registration>().Query()
                .Where(r => allScheduleIds.Contains(r.CourseScheduleId)
                         && (r.Status == RegistrationStatus.Registered
                          || r.Status == RegistrationStatus.Confirmed
                          || r.Status == RegistrationStatus.Completed))
                .GroupBy(r => r.CourseScheduleId)
                .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ScheduleId, g => g.Count)
            : new Dictionary<int, int>();

        foreach (var course in courses)
        {
            var registered = counts.Where(x => course.Schedules.Any(s => s.Id == x.Key)).Sum(x => x.Value);
            var fees = registered * (course.FeePerTrainee ?? 0);
            var ministry = fees * MinistryDeductionRate;

            viewModel.CourseCards.Add(new CourseAccountsCard
            {
                CourseId = course.Id,
                CourseName = course.NameAr,
                CourseCode = course.Code,
                TraineeCount = registered,
                TotalFees = fees,
                MinistryDeduction = ministry,
                NetAfterMinistry = fees - ministry
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _unitOfWork.Repository<Course>().Query()
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        var scheduleIds = course.Schedules.Select(s => s.Id).ToList();

        var registrationCount = await _unitOfWork.Repository<Registration>().Query()
            .CountAsync(r => scheduleIds.Contains(r.CourseScheduleId)
                          && (r.Status == RegistrationStatus.Registered
                           || r.Status == RegistrationStatus.Confirmed
                           || r.Status == RegistrationStatus.Completed));

        var instructorCost = 0m;
        foreach (var schedule in course.Schedules.Where(s => s.Status != ScheduleStatus.Cancelled && s.InstructorId != 0))
        {
            var instructor = await _unitOfWork.Repository<Instructor>().GetByIdAsync(schedule.InstructorId);
            if (instructor == null) continue;
            var totalHours = (decimal)(schedule.EndDate - schedule.StartDate).TotalDays * (decimal)(schedule.EndTime - schedule.StartTime).TotalHours;
            var hours = totalHours;
            instructorCost += hours * instructor.HourlyRate;
        }

        var totalFees = await _unitOfWork.Repository<Registration>().Query()
            .Where(r => scheduleIds.Contains(r.CourseScheduleId))
            .SumAsync(r => r.FeeAmount ?? 0);

        var totalPaid = await _unitOfWork.Repository<Registration>().Query()
            .Where(r => scheduleIds.Contains(r.CourseScheduleId))
            .SumAsync(r => r.AmountPaid ?? 0);

        var ministryDeduction = totalFees * MinistryDeductionRate;
        var netProfit = (totalFees - ministryDeduction) - instructorCost;

        var transactions = await _unitOfWork.Repository<FinanceTransaction>().Query()
            .Include(t => t.CreatedBy)
            .Where(t => t.CourseId == course.Id)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        var trainees = await _unitOfWork.Repository<Registration>().Query()
            .Include(r => r.Trainee)
                .ThenInclude(t => t.User)
            .Where(r => scheduleIds.Contains(r.CourseScheduleId))
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();

        var viewModel = new CourseAccountsViewModel
        {
            Course = course,
            TraineeCount = registrationCount,
            TotalFees = totalFees,
            TotalPaid = totalPaid,
            TotalUnpaid = totalFees - totalPaid,
            InstructorCost = instructorCost,
            MinistryDeduction = ministryDeduction,
            Transactions = transactions,
            TraineeStatements = trainees.Select(r => new TraineeStatementRow
            {
                TraineeName = r.Trainee?.User?.FullNameAr ?? "-",
                EmployeeNumber = r.Trainee?.EmployeeNumber,
                FeeAmount = r.FeeAmount,
                AmountPaid = r.AmountPaid,
                PaymentStatusLabel = PaymentStatusAr(r.PaymentStatus)
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTransaction(FinanceTransaction model)
    {
        var course = await _unitOfWork.Repository<Course>().GetByIdAsync(model.CourseId);
        if (course == null) return NotFound();

        model.CreatedById = _userManager.GetUserId(User);
        model.TransactionDate = DateTime.UtcNow;

        await _unitOfWork.Repository<FinanceTransaction>().AddAsync(model);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم تسجيل العملية المالية";
        return RedirectToAction(nameof(Details), new { id = model.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTransaction(int id, int courseId)
    {
        var transaction = await _unitOfWork.Repository<FinanceTransaction>().GetByIdAsync(id);
        if (transaction == null) return NotFound();

        _unitOfWork.Repository<FinanceTransaction>().Delete(transaction);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف العملية المالية";
        return RedirectToAction(nameof(Details), new { id = courseId });
    }

    private static string PaymentStatusAr(PaymentStatus status)
        => status switch
        {
            PaymentStatus.Paid => "مسدد بالكامل",
            PaymentStatus.Partial => "مسدد جزئياً",
            PaymentStatus.Unpaid => "غير مسدد",
            PaymentStatus.Pending => "قيد الانتظار",
            PaymentStatus.Approved => "معتمد",
            _ => status.ToString()
        };
}