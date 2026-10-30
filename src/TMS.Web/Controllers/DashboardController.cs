using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Execution;
using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;

namespace TMS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

    public async Task<IActionResult> Index(string? from, string? to)
    {
        var fromD = ParseDate(from);
        var toExclusive = ParseDate(to)?.AddDays(1);

        var activeCourses = await _unitOfWork.Repository<Course>().CountAsync(c => c.IsActive);
        var trainees = await _unitOfWork.Repository<Trainee>().CountAsync(t => t.IsActive);
        var instructors = await _unitOfWork.Repository<Instructor>().CountAsync(i => i.IsActive);

        IQueryable<CourseSchedule> Sched()
        {
            var q = _unitOfWork.Repository<CourseSchedule>().Query();
            if (fromD.HasValue)
                q = q.Where(s => s.EndDate >= fromD.Value);
            if (toExclusive.HasValue)
                q = q.Where(s => s.StartDate < toExclusive.Value);
            return q;
        }

        IQueryable<Registration> Regs()
        {
            var q = _unitOfWork.Repository<Registration>().Query();
            if (fromD.HasValue)
                q = q.Where(r => r.CourseSchedule.EndDate >= fromD.Value);
            if (toExclusive.HasValue)
                q = q.Where(r => r.CourseSchedule.StartDate < toExclusive.Value);
            return q;
        }

        IQueryable<Course> CoursesInRange()
        {
            var q = _unitOfWork.Repository<Course>().Query().Where(c => c.IsActive);
            if (fromD.HasValue)
                q = q.Where(c => c.Schedules.Any(s => s.EndDate >= fromD.Value));
            if (toExclusive.HasValue)
                q = q.Where(c => c.Schedules.Any(s => s.StartDate < toExclusive.Value));
            return q;
        }

        IQueryable<Certificate> Certs()
        {
            var q = _unitOfWork.Repository<Certificate>().Query();
            if (fromD.HasValue)
                q = q.Where(c => c.IssueDate >= fromD.Value);
            if (toExclusive.HasValue)
                q = q.Where(c => c.IssueDate < toExclusive.Value);
            return q;
        }

        IQueryable<Attendance> Att()
        {
            var q = _unitOfWork.Repository<Attendance>().Query();
            if (fromD.HasValue)
                q = q.Where(a => a.Date >= fromD.Value);
            if (toExclusive.HasValue)
                q = q.Where(a => a.Date < toExclusive.Value);
            return q;
        }

        var schedQuery = Sched();
        var activeSchedules = await schedQuery.CountAsync(s => s.Status == ScheduleStatus.Active);
        var completedSchedules = await schedQuery.CountAsync(s => s.Status == ScheduleStatus.Completed);
        var totalSchedules = await schedQuery.CountAsync();

        var regsQuery = Regs();
        var totalRegistrations = await regsQuery.CountAsync();
        var pendingRegistrations = await regsQuery.CountAsync(r => r.Status == RegistrationStatus.Waitlisted);

        var fees = await regsQuery
            .Select(r => new { Total = r.FeeAmount ?? 0m, Paid = r.AmountPaid ?? 0m })
            .ToListAsync();
        var feesTotal = fees.Sum(f => f.Total);
        var paidTotal = fees.Sum(f => f.Paid);
        var outstandingTotal = feesTotal - paidTotal;

        var attendanceRecords = await Att().CountAsync();
        var certificates = await Certs().CountAsync();

        var upcomingSchedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Where(s => s.StartDate >= DateTime.Today && s.Status == ScheduleStatus.Scheduled)
            .OrderBy(s => s.StartDate)
            .Take(5)
            .ToListAsync();

        var topCourses = await regsQuery
            .Include(r => r.CourseSchedule)
            .ThenInclude(s => s.Course)
            .Where(r => r.CourseSchedule.Course.IsActive)
            .GroupBy(r => new { r.CourseSchedule.CourseId, r.CourseSchedule.Course.NameAr })
            .Select(g => new { g.Key.NameAr, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();

        var recentCertificates = await Certs()
            .Include(c => c.CourseSchedule)
            .ThenInclude(s => s.Course)
            .Include(c => c.Trainee)
            .ThenInclude(t => t.User)
            .OrderByDescending(c => c.IssueDate)
            .Take(5)
            .ToListAsync();

        var coursesQuery = CoursesInRange();

        var courseStatusCounts = await coursesQuery
            .GroupBy(c => c.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var categoryCounts = await coursesQuery
            .Include(c => c.Category)
            .Where(c => c.Category != null)
            .GroupBy(c => c.Category!.NameAr)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(8)
            .ToListAsync();

        var scheduleStatusCounts = await schedQuery
            .GroupBy(s => s.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var monthStarts = BuildMonthStarts(fromD, toExclusive);
        var monthlyCourses = new List<int>();
        var monthlyRegistrations = new List<int>();
        var monthlyCertificates = new List<int>();
        foreach (var m in monthStarts)
        {
            var start = m;
            var end = m.AddMonths(1);
            monthlyCourses.Add(await _unitOfWork.Repository<Course>().CountAsync(c =>
                c.CreatedAt >= start && c.CreatedAt < end &&
                (!fromD.HasValue || c.CreatedAt >= fromD.Value) &&
                (!toExclusive.HasValue || c.CreatedAt < toExclusive.Value)));
            monthlyRegistrations.Add(await _unitOfWork.Repository<Registration>().CountAsync(r =>
                r.RegisteredAt >= start && r.RegisteredAt < end &&
                (!fromD.HasValue || r.RegisteredAt >= fromD.Value) &&
                (!toExclusive.HasValue || r.RegisteredAt < toExclusive.Value)));
            monthlyCertificates.Add(await _unitOfWork.Repository<Certificate>().CountAsync(c =>
                c.IssueDate >= start && c.IssueDate < end &&
                (!fromD.HasValue || c.IssueDate >= fromD.Value) &&
                (!toExclusive.HasValue || c.IssueDate < toExclusive.Value)));
        }

        var filtered = fromD.HasValue || toExclusive.HasValue;

        ViewBag.ActiveCourses = activeCourses;
        ViewBag.Trainees = trainees;
        ViewBag.Instructors = instructors;
        ViewBag.Certificates = certificates;
        ViewBag.ActiveSchedules = activeSchedules;
        ViewBag.UpcomingSchedules = upcomingSchedules;

        ViewBag.TotalSchedules = totalSchedules;
        ViewBag.CompletedSchedules = completedSchedules;
        ViewBag.Registrations = totalRegistrations;
        ViewBag.PendingRegistrations = pendingRegistrations;
        ViewBag.AttendanceRecords = attendanceRecords;
        ViewBag.FeesTotal = feesTotal;
        ViewBag.PaidTotal = paidTotal;
        ViewBag.OutstandingTotal = outstandingTotal;

        ViewBag.FromFilter = from;
        ViewBag.ToFilter = to;
        ViewBag.Filtered = filtered;
        ViewBag.MonthlyLabels = monthStarts.Select(m => m.ToString("MMM")).ToArray();
        ViewBag.MonthlyCourses = monthlyCourses;
        ViewBag.MonthlyRegistrations = monthlyRegistrations;
        ViewBag.MonthlyCertificates = monthlyCertificates;

        ViewBag.CategoryLabels = categoryCounts.Select(c => c.Name).ToList();
        ViewBag.CategoryCounts = categoryCounts.Select(c => c.Count).ToList();
        ViewBag.CourseStatusKeys = courseStatusCounts.Select(c => c.Key.ToString()).ToList();
        ViewBag.CourseStatusCounts = courseStatusCounts.Select(c => c.Count).ToList();
        ViewBag.ScheduleStatusKeys = scheduleStatusCounts.Select(s => s.Key.ToString()).ToList();
        ViewBag.ScheduleStatusCounts = scheduleStatusCounts.Select(s => s.Count).ToList();
        ViewBag.TopCourses = topCourses;
        ViewBag.RecentCertificates = recentCertificates;
        ViewBag.AverageTraineesPerSchedule = totalSchedules == 0 ? 0 : totalRegistrations / (double)totalSchedules;

        return View();
    }

    private static List<DateTime> BuildMonthStarts(DateTime? fromD, DateTime? toExclusive)
    {
        if (fromD.HasValue || toExclusive.HasValue)
        {
            var start = fromD.HasValue
                ? new DateTime(fromD.Value.Year, fromD.Value.Month, 1)
                : DateTime.Today.AddMonths(-5);
            var end = toExclusive.HasValue
                ? toExclusive.Value.AddMonths(-1).Date
                : DateTime.Today;

            var list = new List<DateTime>();
            var m = start;
            while (m <= end && list.Count < 24)
            {
                list.Add(m);
                m = m.AddMonths(1);
            }
            if (list.Count > 0)
                return list;
        }

        return Enumerable.Range(0, 6)
            .Select(i => new DateTime(DateTime.Today.AddMonths(i - 5).Year, DateTime.Today.AddMonths(i - 5).Month, 1))
            .ToList();
    }
}