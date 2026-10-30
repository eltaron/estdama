using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Globalization;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Execution;
using TMS.Core.Entities.Instructors;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Infrastructure.Services;
using TMS.Web.ViewModels.Reports;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.ReportsView)]
public class ReportsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _export;
    private readonly IStringLocalizer<Shared> _t;

    public ReportsController(IUnitOfWork unitOfWork, IExportService export, IStringLocalizer<Shared> t)
    {
        _unitOfWork = unitOfWork;
        _export = export;
        _t = t;
    }

    private IQueryable<CourseSchedule> SchedulesByCourseScheduleFilters(IQueryable<CourseSchedule> q, int? categoryId, int? courseId, int? year, string? from, string? to)
    {
        if (courseId.HasValue)
            q = q.Where(s => s.CourseId == courseId.Value);
        if (categoryId.HasValue)
            q = q.Where(s => s.Course.CategoryId == categoryId.Value);
        if (year.HasValue)
            q = q.Where(s => s.StartDate.Year == year.Value);
        var fromD = ParseDate(from);
        var toD = ParseDate(to);
        if (fromD.HasValue)
            q = q.Where(s => s.EndDate >= fromD.Value);
        if (toD.HasValue)
            q = q.Where(s => s.StartDate <= toD.Value);
        return q;
    }

    private async Task PopulateReportBagsAsync(int? categoryId = null, int? courseId = null, int? year = null, string? from = null, string? to = null, string? search = null, bool includeCourses = true)
    {
        var categories = await _unitOfWork.Repository<CourseCategory>()
            .Query()
            .Where(c => c.IsActive)
            .OrderBy(c => c.NameAr)
            .ToListAsync();

        ViewBag.Categories = new SelectList(categories, "Id", "NameAr");
        ViewBag.Years = await GetYearsAsync();
        ViewBag.CategoryFilter = categoryId;
        ViewBag.YearFilter = year;
        ViewBag.FromFilter = from;
        ViewBag.ToFilter = to;
        ViewBag.SearchFilter = search;

        if (includeCourses)
        {
            var courses = await _unitOfWork.Repository<Course>()
                .Query()
                .Where(c => c.IsActive)
                .OrderBy(c => c.NameAr)
                .ToListAsync();

            ViewBag.Courses = new SelectList(
                courses.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.NameAr} ({c.Code})"
                }),
                "Value",
                "Text");
        }
        ViewBag.CourseFilter = courseId;
    }

    private async Task<List<int>> GetYearsAsync()
    {
        var scheduleYears = await _unitOfWork.Repository<CourseSchedule>().Query().Select(s => s.StartDate.Year).Distinct().ToListAsync();
        var registrationYears = await _unitOfWork.Repository<Registration>().Query().Select(r => r.RegisteredAt.Year).Distinct().ToListAsync();
        var certificateYears = await _unitOfWork.Repository<Certificate>().Query().Select(c => c.IssueDate.Year).Distinct().ToListAsync();
        var examYears = await _unitOfWork.Repository<Exam>().Query().Select(e => e.StartTime.Year).Distinct().ToListAsync();
        return scheduleYears.Concat(registrationYears).Concat(certificateYears).Concat(examYears).Distinct().OrderByDescending(y => y).ToList();
    }

    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

    private IActionResult ToExport(string? format, string title, string[] headers, List<object?[]> rows, string stem)
    {
        if (format == "excel")
            return File(_export.Excel(title, headers, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{stem}_{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.xlsx");
        if (format == "pdf")
            return File(_export.Pdf(title, headers, rows), "application/pdf", $"{stem}_{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.pdf");
        return BadRequest();
    }

    private string[] Col(params string[] keys) => keys.Select(k => _t[k].Value).ToArray();

    private string CourseStatusText(CourseStatus s) => s switch
    {
        CourseStatus.Planned => _t["Courses.StatusPlanned"].Value,
        CourseStatus.OpenForRegistration => _t["Courses.StatusOpen"].Value,
        CourseStatus.InProgress => _t["Courses.StatusInProgress"].Value,
        CourseStatus.Completed => _t["Courses.StatusCompleted"].Value,
        CourseStatus.Cancelled => _t["Courses.StatusCancelled"].Value,
        _ => s.ToString()
    };

    private string RegistrationStatusText(RegistrationStatus s) => s switch
    {
        RegistrationStatus.Registered => _t["Registrations.StatusRegistered"].Value,
        RegistrationStatus.Confirmed => _t["Registrations.StatusConfirmed"].Value,
        RegistrationStatus.Completed => _t["Registrations.StatusCompleted"].Value,
        RegistrationStatus.Dropped => _t["Registrations.StatusDropped"].Value,
        RegistrationStatus.Waitlisted => _t["Registrations.StatusWaitlisted"].Value,
        _ => s.ToString()
    };

    private string PaymentStatusText(PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => _t["Reports.PayPending"].Value,
        PaymentStatus.Approved => _t["Reports.PayApproved"].Value,
        PaymentStatus.Paid => _t["Registrations.PaymentPaid"].Value,
        PaymentStatus.Partial => _t["Registrations.PaymentPartial"].Value,
        PaymentStatus.Unpaid => _t["Registrations.PaymentUnpaid"].Value,
        _ => s.ToString()
    };

    private string ScheduleStatusText(ScheduleStatus s) => s switch
    {
        ScheduleStatus.Scheduled => _t["Courses.ScheduleScheduled"].Value,
        ScheduleStatus.Active => _t["Courses.StatusInProgress"].Value,
        ScheduleStatus.Completed => _t["Courses.StatusCompleted"].Value,
        ScheduleStatus.Cancelled => _t["Courses.StatusCancelled"].Value,
        _ => s.ToString()
    };

    private string GradeText(string gradeKey) => _t[gradeKey].Value;

    public async Task<IActionResult> Index()
    {
        var totalCourses = await _unitOfWork.Repository<Course>().CountAsync(c => c.IsActive);
        var totalSchedules = await _unitOfWork.Repository<CourseSchedule>().CountAsync();
        var completedSchedules = await _unitOfWork.Repository<CourseSchedule>().CountAsync(s => s.Status == ScheduleStatus.Completed);
        var activeSchedules = await _unitOfWork.Repository<CourseSchedule>().CountAsync(s => s.Status == ScheduleStatus.Active);
        var totalRegistrations = await _unitOfWork.Repository<Registration>().CountAsync();
        var pendingRegistrations = await _unitOfWork.Repository<Registration>().CountAsync(r => r.Status == RegistrationStatus.Waitlisted);
        var totalCertificates = await _unitOfWork.Repository<Certificate>().CountAsync();
        var totalInstructors = await _unitOfWork.Repository<Instructor>().CountAsync(i => i.IsActive);
        var totalTrainees = await _unitOfWork.Repository<Trainee>().CountAsync(t => t.IsActive);
        var attendanceRecords = await _unitOfWork.Repository<Attendance>().CountAsync();

        var statusCounts = await _unitOfWork.Repository<Course>()
            .Query()
            .Where(c => c.IsActive)
            .GroupBy(c => c.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var categoryCounts = await _unitOfWork.Repository<Course>()
            .Query()
            .Include(c => c.Category)
            .Where(c => c.IsActive && c.Category != null)
            .GroupBy(c => c.Category!.NameAr)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(8)
            .ToListAsync();

        var topCourses = await _unitOfWork.Repository<Registration>()
            .Query()
            .Include(r => r.CourseSchedule)
            .ThenInclude(s => s.Course)
            .Where(r => r.Status == RegistrationStatus.Registered && r.CourseSchedule != null && r.CourseSchedule.Course != null)
            .GroupBy(r => new { r.CourseSchedule.CourseId, r.CourseSchedule.Course.NameAr })
            .Select(g => new { g.Key.NameAr, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();

        var schedulesByStatus = await _unitOfWork.Repository<CourseSchedule>()
            .Query()
            .GroupBy(s => s.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        ViewBag.TotalCourses = totalCourses;
        ViewBag.TotalSchedules = totalSchedules;
        ViewBag.CompletedSchedules = completedSchedules;
        ViewBag.ActiveSchedules = activeSchedules;
        ViewBag.TotalRegistrations = totalRegistrations;
        ViewBag.PendingRegistrations = pendingRegistrations;
        ViewBag.TotalCertificates = totalCertificates;
        ViewBag.TotalInstructors = totalInstructors;
        ViewBag.TotalTrainees = totalTrainees;
        ViewBag.AttendanceRecords = attendanceRecords;

        ViewBag.StatusLabels = statusCounts.Select(s => s.Key.ToString()).ToList();
        ViewBag.StatusCounts = statusCounts.Select(s => s.Count).ToList();
        ViewBag.CategoryLabels = categoryCounts.Select(c => c.Name).ToList();
        ViewBag.CategoryCounts = categoryCounts.Select(c => c.Count).ToList();
        ViewBag.ScheduleStatusLabels = schedulesByStatus.Select(s => s.Key.ToString()).ToList();
        ViewBag.ScheduleStatusCounts = schedulesByStatus.Select(s => s.Count).ToList();

        ViewBag.TopCourses = topCourses;
        ViewBag.RegistrationRate = totalSchedules == 0
            ? 0
            : (int)Math.Round(completedSchedules * 100.0 / totalSchedules);
        ViewBag.AverageTraineesPerSchedule = totalSchedules == 0
            ? 0
            : (totalRegistrations / (double)totalSchedules);

        return View();
    }

    public async Task<IActionResult> CourseReport(int? categoryId, int? year, CourseStatus? status, string? search, string? export)
    {
        IQueryable<Course> q = _unitOfWork.Repository<Course>().Query().Where(c => c.IsActive);

        if (categoryId.HasValue)
            q = q.Where(c => c.CategoryId == categoryId.Value);
        if (status.HasValue)
            q = q.Where(c => c.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.NameAr.Contains(search) || c.NameEn.Contains(search) || c.Code.Contains(search));
        if (year.HasValue)
            q = q.Where(c => c.Schedules.Any(s => s.StartDate.Year == year.Value));

        var courses = await q
            .Include(c => c.Category)
            .Include(c => c.Schedules)
            .OrderBy(c => c.NameAr)
            .ToListAsync();

        var courseIds = courses.Select(c => c.Id).ToList();
        var regCounts = new Dictionary<int, int>();
        if (courseIds.Count > 0)
        {
            regCounts = await _unitOfWork.Repository<Registration>()
                .Query()
                .Where(r => courseIds.Contains(r.CourseSchedule.CourseId))
                .GroupBy(r => r.CourseSchedule.CourseId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
        }

        var rows = courses.Select(c => new CourseReportRow
        {
            Code = c.Code,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            CategoryName = c.Category?.NameAr,
            Hours = c.Hours,
            Status = c.Status,
            ScheduleCount = c.Schedules.Count,
            RegistrationCount = regCounts.GetValueOrDefault(c.Id)
        }).ToList();

        await PopulateReportBagsAsync(categoryId: categoryId, year: year, search: search, includeCourses: false);
        ViewBag.StatusFilter = status;

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.CoursesReport"].Value,
                Col("Courses.Code", "Reports.Course", "Reports.Category", "Courses.Hours", "Common.Status", "Reports.ScheduleCount", "Reports.RegisteredCount"),
                rows.Select(c => new object?[] { c.Code, c.NameAr, c.CategoryName, c.Hours, CourseStatusText(c.Status), c.ScheduleCount, c.RegistrationCount }).ToList(),
                "CourseReport");

        return View(rows);
    }

    public async Task<IActionResult> RegistrationReport(int? categoryId, int? courseId, int? year, string? from, string? to, RegistrationStatus? status, string? export)
    {
        IQueryable<Registration> q = _unitOfWork.Repository<Registration>().Query();

        if (courseId.HasValue)
            q = q.Where(r => r.CourseSchedule.CourseId == courseId.Value);
        if (categoryId.HasValue)
            q = q.Where(r => r.CourseSchedule.Course.CategoryId == categoryId.Value);
        if (year.HasValue)
            q = q.Where(r => r.RegisteredAt.Year == year.Value);
        var fromD = ParseDate(from);
        var toD = ParseDate(to);
        if (fromD.HasValue)
            q = q.Where(r => r.RegisteredAt >= fromD.Value);
        if (toD.HasValue)
            q = q.Where(r => r.RegisteredAt < toD.Value.AddDays(1));
        if (status.HasValue)
            q = q.Where(r => r.Status == status.Value);

        var rows = await q
            .OrderByDescending(r => r.RegisteredAt)
            .Take(500)
            .Select(r => new RegistrationReportRow
            {
                TraineeName = r.Trainee.User.FullNameAr,
                CourseName = r.CourseSchedule.Course.NameAr,
                InstructorName = r.CourseSchedule.Instructor.FirstNameAr + " " + r.CourseSchedule.Instructor.LastNameAr,
                StartDate = r.CourseSchedule.StartDate,
                EndDate = r.CourseSchedule.EndDate,
                RegisteredAt = r.RegisteredAt,
                Status = r.Status,
                FeeAmount = r.FeeAmount,
                AmountPaid = r.AmountPaid,
                PaymentStatus = r.PaymentStatus
            })
            .ToListAsync();

        await PopulateReportBagsAsync(categoryId: categoryId, courseId: courseId, year: year, from: from, to: to);
        ViewBag.StatusFilter = status;

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.RegistrationsReport"].Value,
                Col("Registrations.Trainee", "Reports.Course", "Instructors.Instructor", "Reports.Period", "Reports.EnrolledDate", "Common.Status", "Registrations.Fee", "Registrations.Paid", "Registrations.PaymentStatus"),
                rows.Select(r => new object?[]
                {
                    r.TraineeName, r.CourseName, r.InstructorName,
                    $"{r.StartDate:yyyy-MM-dd} - {r.EndDate:yyyy-MM-dd}",
                    r.RegisteredAt?.ToString("yyyy-MM-dd") ?? "",
                    RegistrationStatusText(r.Status), r.FeeAmount, r.AmountPaid, PaymentStatusText(r.PaymentStatus)
                }).ToList(),
                "RegistrationReport");

        return View(rows);
    }

    public async Task<IActionResult> FinanceReport(int? categoryId, int? courseId, int? year, string? from, string? to, PaymentStatus? status, string? export)
    {
        IQueryable<Registration> q = _unitOfWork.Repository<Registration>().Query();

        if (courseId.HasValue)
            q = q.Where(r => r.CourseSchedule.CourseId == courseId.Value);
        if (categoryId.HasValue)
            q = q.Where(r => r.CourseSchedule.Course.CategoryId == categoryId.Value);
        if (year.HasValue)
            q = q.Where(r => r.CourseSchedule.StartDate.Year == year.Value);
        var fromD = ParseDate(from);
        var toD = ParseDate(to);
        if (fromD.HasValue)
            q = q.Where(r => r.CourseSchedule.EndDate >= fromD.Value);
        if (toD.HasValue)
            q = q.Where(r => r.CourseSchedule.StartDate <= toD.Value);
        if (status.HasValue)
            q = q.Where(r => r.PaymentStatus == status.Value);

        var rows = await q
            .GroupBy(r => new { r.CourseScheduleId, r.CourseSchedule.StartDate, r.CourseSchedule.EndDate, CourseName = r.CourseSchedule.Course.NameAr })
            .Select(g => new FinanceReportRow
            {
                CourseName = g.Key.CourseName,
                StartDate = g.Key.StartDate,
                EndDate = g.Key.EndDate,
                Registrations = g.Count(),
                TotalFees = g.Sum(r => r.FeeAmount),
                TotalPaid = g.Sum(r => r.AmountPaid),
                Outstanding = g.Sum(r => r.FeeAmount) - g.Sum(r => r.AmountPaid)
            })
            .OrderByDescending(r => r.EndDate)
            .ToListAsync();

        await PopulateReportBagsAsync(categoryId: categoryId, courseId: courseId, year: year, from: from, to: to);
        ViewBag.StatusFilter = status;

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.FinanceReport"].Value,
                Col("Reports.Course", "Reports.Period", "Reports.RegisteredCount", "Finance.TotalFees", "Finance.Paid", "Finance.Remaining"),
                rows.Select(r => new object?[]
                {
                    r.CourseName, $"{r.StartDate:yyyy-MM-dd} - {r.EndDate:yyyy-MM-dd}",
                    r.Registrations, r.TotalFees, r.TotalPaid, r.Outstanding
                }).ToList(),
                "FinanceReport");

        return View(rows);
    }

    public async Task<IActionResult> AttendanceReport(int? categoryId, int? courseId, int? year, string? from, string? to, string? export)
    {
        IQueryable<Attendance> q = _unitOfWork.Repository<Attendance>().Query();

        if (courseId.HasValue)
            q = q.Where(a => a.CourseSchedule.CourseId == courseId.Value);
        if (categoryId.HasValue)
            q = q.Where(a => a.CourseSchedule.Course.CategoryId == categoryId.Value);
        if (year.HasValue)
            q = q.Where(a => a.Date.Year == year.Value);
        var fromD = ParseDate(from);
        var toD = ParseDate(to);
        if (fromD.HasValue)
            q = q.Where(a => a.Date >= fromD.Value);
        if (toD.HasValue)
            q = q.Where(a => a.Date < toD.Value.AddDays(1));

        var rows = await q
            .GroupBy(a => new { a.CourseScheduleId, a.CourseSchedule.StartDate, a.CourseSchedule.EndDate, CourseName = a.CourseSchedule.Course.NameAr })
            .Select(g => new AttendanceReportRow
            {
                CourseName = g.Key.CourseName,
                StartDate = g.Key.StartDate,
                EndDate = g.Key.EndDate,
                Records = g.Count(),
                Sessions = g.Select(a => a.Date).Distinct().Count(),
                Present = g.Count(a => a.Status == AttendanceStatus.Present),
                Absent = g.Count(a => a.Status != AttendanceStatus.Present)
            })
            .OrderByDescending(r => r.EndDate)
            .ToListAsync();

        foreach (var row in rows)
            row.AttendanceRate = row.Records == 0 ? 0 : (int)Math.Round(row.Present * 100.0 / row.Records);

        await PopulateReportBagsAsync(categoryId: categoryId, courseId: courseId, year: year, from: from, to: to);

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.AttendanceReport"].Value,
                Col("Reports.Course", "Reports.Period", "Reports.Records", "Reports.Sessions", "Attendance.Present", "Attendance.Absent", "Reports.AttendanceRate"),
                rows.Select(r => new object?[]
                {
                    r.CourseName, $"{r.StartDate:yyyy-MM-dd} - {r.EndDate:yyyy-MM-dd}",
                    r.Records, r.Sessions, r.Present, r.Absent, $"{r.AttendanceRate}%"
                }).ToList(),
                "AttendanceReport");

        return View(rows);
    }

    public async Task<IActionResult> ExamReport(int? categoryId, int? courseId, int? year, string? from, string? to, string? export)
    {
        IQueryable<Exam> q = _unitOfWork.Repository<Exam>().Query();

        if (courseId.HasValue)
            q = q.Where(e => e.CourseSchedule.CourseId == courseId.Value);
        if (categoryId.HasValue)
            q = q.Where(e => e.CourseSchedule.Course.CategoryId == categoryId.Value);
        if (year.HasValue)
            q = q.Where(e => e.StartTime.Year == year.Value);
        var fromD = ParseDate(from);
        var toD = ParseDate(to);
        if (fromD.HasValue)
            q = q.Where(e => e.StartTime >= fromD.Value);
        if (toD.HasValue)
            q = q.Where(e => e.StartTime < toD.Value.AddDays(1));

        var exams = await q
            .Include(e => e.CourseSchedule)
            .ThenInclude(s => s.Course)
            .Include(e => e.Submissions)
            .OrderByDescending(e => e.StartTime)
            .Take(500)
            .ToListAsync();

        var rows = exams.Select(e =>
        {
            var submissions = e.Submissions.Where(s => s.Status == SubmissionStatus.Completed).ToList();
            var avg = submissions.Count == 0 ? 0m : Math.Round(submissions.Average(s => s.TotalScore), 1);
            var passed = submissions.Count(s => s.IsPassed);
            return new ExamReportRow
            {
                CourseName = e.CourseSchedule?.Course?.NameAr ?? "",
                TitleAr = e.TitleAr,
                TitleEn = e.TitleEn,
                ExamDate = e.StartTime,
                TotalMarks = e.TotalMarks,
                PassingMarks = e.PassingMarks,
                Submissions = submissions.Count,
                AvgScore = avg,
                Passed = passed,
                Failed = submissions.Count - passed,
                PassRate = submissions.Count == 0 ? 0 : (int)Math.Round(passed * 100.0 / submissions.Count)
            };
        }).ToList();

        await PopulateReportBagsAsync(categoryId: categoryId, courseId: courseId, year: year, from: from, to: to);

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.ExamsReport"].Value,
                Col("Reports.Course", "Exams.TitleLabel", "Exams.StartTime", "Exams.TotalMarks", "Exams.PassingMarks", "Exams.SubmissionsCount", "Reports.AvgScore", "Reports.Passed", "Reports.Failed", "Reports.PassRate"),
                rows.Select(e => new object?[]
                {
                    e.CourseName, e.TitleAr, e.ExamDate?.ToString("yyyy-MM-dd") ?? "", e.TotalMarks, e.PassingMarks,
                    e.Submissions, e.AvgScore, e.Passed, e.Failed, $"{e.PassRate}%"
                }).ToList(),
                "ExamReport");

        return View(rows);
    }

    public async Task<IActionResult> CertificateReport(int? categoryId, int? courseId, int? year, string? from, string? to, string? search, string? export)
    {
        IQueryable<Certificate> q = _unitOfWork.Repository<Certificate>().Query();

        if (courseId.HasValue)
            q = q.Where(c => c.CourseSchedule.CourseId == courseId.Value);
        if (categoryId.HasValue)
            q = q.Where(c => c.CourseSchedule.Course.CategoryId == categoryId.Value);
        if (year.HasValue)
            q = q.Where(c => c.IssueDate.Year == year.Value);
        var fromD = ParseDate(from);
        var toD = ParseDate(to);
        if (fromD.HasValue)
            q = q.Where(c => c.IssueDate >= fromD.Value);
        if (toD.HasValue)
            q = q.Where(c => c.IssueDate < toD.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.SerialNumber.Contains(search));

        var certificates = await q
            .Include(c => c.CourseSchedule)
            .ThenInclude(s => s.Course)
            .Include(c => c.Trainee)
            .ThenInclude(t => t.User)
            .OrderByDescending(c => c.IssueDate)
            .Take(500)
            .ToListAsync();

        await PopulateReportBagsAsync(categoryId: categoryId, courseId: courseId, year: year, from: from, to: to, search: search);

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.CertificatesReport"].Value,
                Col("Certificates.SerialNumber", "Certificates.Trainee", "Certificates.Course", "Certificates.IssueDate", "Common.Status"),
                certificates.Select(c => new object?[]
                {
                    c.SerialNumber, c.Trainee?.User?.FullNameAr ?? "", c.CourseSchedule?.Course?.NameAr ?? "",
                    c.IssueDate.ToString("yyyy-MM-dd"),
                    c.IsRevoked ? _t["Reports.StatusRevoked"].Value : _t["Reports.StatusValid"].Value
                }).ToList(),
                "CertificateReport");

        return View(certificates);
    }

    public async Task<IActionResult> InstructorReport(int? categoryId, int? courseId, int? year, string? from, string? to, string? export)
    {
        IQueryable<CourseSchedule> q = _unitOfWork.Repository<CourseSchedule>().Query();
        q = SchedulesByCourseScheduleFilters(q, categoryId, courseId, year, from, to);

        var schedules = await q
            .Include(s => s.Instructor)
            .Include(s => s.Course)
            .ToListAsync();

        var scheduleIds = schedules.Select(s => s.Id).ToList();
        var regCounts = new Dictionary<int, int>();
        if (scheduleIds.Count > 0)
        {
            regCounts = await _unitOfWork.Repository<Registration>()
                .Query()
                .Where(r => scheduleIds.Contains(r.CourseScheduleId))
                .GroupBy(r => r.CourseScheduleId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
        }

        var rows = schedules
            .Where(s => s.Instructor != null)
            .GroupBy(s => s.Instructor!.Id)
            .Select(g =>
            {
                var first = g.First();
                return new InstructorReportRow
                {
                    InstructorName = first.Instructor!.FirstNameAr + " " + first.Instructor.LastNameAr,
                    ScheduleCount = g.Count(),
                    CourseCount = g.Select(s => s.CourseId).Distinct().Count(),
                    TotalDays = g.Sum(s => (s.EndDate - s.StartDate).Days + 1),
                    TotalRegistrations = g.Sum(s => regCounts.GetValueOrDefault(s.Id))
                };
            })
            .OrderByDescending(r => r.ScheduleCount)
            .ToList();

        await PopulateReportBagsAsync(categoryId: categoryId, courseId: courseId, year: year, from: from, to: to);

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.InstructorsReport"].Value,
                Col("Instructors.Instructor", "Reports.ScheduleCount", "Reports.TotalCourses", "Reports.Days", "Reports.RegisteredCount"),
                rows.Select(r => new object?[]
                {
                    r.InstructorName, r.ScheduleCount, r.CourseCount, r.TotalDays, r.TotalRegistrations
                }).ToList(),
                "InstructorReport");

        return View(rows);
    }

    private const string GradeExcellent = "Reports.GradeExcellent";
    private const string GradeVeryGood = "Reports.GradeVeryGood";
    private const string GradeGood = "Reports.GradeGood";
    private const string GradeAcceptable = "Reports.GradeAcceptable";
    private const string GradeFail = "Reports.GradeFail";
    private const string GradeNone = "Reports.NotGraded";

    private static string GradeForKey(decimal pct)
    {
        if (pct >= 90) return GradeExcellent;
        if (pct >= 80) return GradeVeryGood;
        if (pct >= 70) return GradeGood;
        if (pct >= 60) return GradeAcceptable;
        return GradeFail;
    }

    public async Task<IActionResult> TraineeReport(int? categoryId, int? courseId, int? traineeType, int? year, string? from, string? to, string? search, string? export)
    {
        IQueryable<Registration> q = _unitOfWork.Repository<Registration>().Query();

        if (courseId.HasValue)
            q = q.Where(r => r.CourseSchedule.CourseId == courseId.Value);
        if (categoryId.HasValue)
            q = q.Where(r => r.CourseSchedule.Course.CategoryId == categoryId.Value);
        if (traineeType.HasValue && Enum.IsDefined(typeof(TraineeType), traineeType.Value))
            q = q.Where(r => r.Trainee.Type == (TraineeType)traineeType.Value);
        if (year.HasValue)
            q = q.Where(r => r.CourseSchedule.StartDate.Year == year.Value);
        var fromD = ParseDate(from);
        var toD = ParseDate(to);
        if (fromD.HasValue)
            q = q.Where(r => r.CourseSchedule.EndDate >= fromD.Value);
        if (toD.HasValue)
            q = q.Where(r => r.CourseSchedule.StartDate <= toD.Value);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.Trainee.User.FullNameAr.Contains(search) || r.Trainee.User.FullNameEn.Contains(search) || (r.Trainee.EmployeeNumber != null && r.Trainee.EmployeeNumber.Contains(search)));

        var regs = await q
            .Include(r => r.Trainee)
            .ThenInclude(t => t.User)
            .Include(r => r.CourseSchedule)
            .ThenInclude(s => s.Course)
            .ThenInclude(c => c!.Category)
            .OrderBy(r => r.Trainee.User.FullNameAr)
            .ThenBy(r => r.CourseSchedule.StartDate)
            .ToListAsync();

        var scheduleIds = regs.Select(r => r.CourseScheduleId).Distinct().ToList();
        var finalExams = new Dictionary<int, Exam>();
        if (scheduleIds.Count > 0)
        {
            var exams = await _unitOfWork.Repository<Exam>()
                .Query()
                .Where(e => scheduleIds.Contains(e.CourseScheduleId) && e.IsActive && e.Type == ExamType.Final)
                .Include(e => e.Submissions)
                .ToListAsync();

            finalExams = exams
                .GroupBy(e => e.CourseScheduleId)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.StartTime).Last());
        }

        var rows = new List<TraineeReportRow>();
        foreach (var r in regs)
        {
            var row = new TraineeReportRow
            {
                TraineeId = r.TraineeId,
                TraineeNameAr = r.Trainee.User.FullNameAr,
                TraineeNameEn = r.Trainee.User.FullNameEn,
                TraineeType = r.Trainee.Type,
                CourseCategoryName = r.CourseSchedule.Course.Category?.NameAr ?? "",
                CourseNameAr = r.CourseSchedule.Course.NameAr,
                CourseNameEn = r.CourseSchedule.Course.NameEn,
                StartDate = r.CourseSchedule.StartDate,
                EndDate = r.CourseSchedule.EndDate
            };

            if (finalExams.TryGetValue(r.CourseScheduleId, out var exam) && exam.TotalMarks > 0)
            {
                var submission = exam.Submissions.FirstOrDefault(s => s.TraineeId == r.TraineeId && s.Status == SubmissionStatus.Completed);
                if (submission != null)
                {
                    row.HasGrade = true;
                    row.Score = submission.TotalScore;
                    row.MaxScore = exam.TotalMarks;
                    row.ScorePct = Math.Round(submission.TotalScore * 100m / exam.TotalMarks, 1);
                    row.GradeKey = GradeForKey(row.ScorePct);
                }
            }

            rows.Add(row);
        }

        var graded = rows.Where(r => r.HasGrade).ToList();
        var model = new TraineeReportViewModel
        {
            Rows = rows,
            TotalTrainees = rows.Select(r => r.TraineeId).Distinct().Count(),
            TotalCourses = rows.Count,
            GradedCount = graded.Count,
            PassedCount = graded.Count(r => r.GradeKey != GradeFail),
            PassRate = graded.Count == 0 ? 0 : (int)Math.Round(graded.Count(r => r.GradeKey != GradeFail) * 100.0 / graded.Count),
            AvgScore = graded.Count == 0 ? 0 : Math.Round(graded.Average(r => r.ScorePct), 1),
            ByCategory = rows
                .GroupBy(r => string.IsNullOrEmpty(r.CourseCategoryName) ? "-" : r.CourseCategoryName)
                .Select(g => new NameCount { Name = g.Key, Count = g.Count() })
                .OrderByDescending(n => n.Count)
                .ToList(),
            ByTraineeType = rows
                .GroupBy(r => r.TraineeType)
                .Select(g => new NameCount
                {
                    Name = g.Key == TraineeType.Private ? "Trainees.PrivateSector" : "Trainees.Government",
                    Count = g.Count()
                })
                .OrderByDescending(n => n.Count)
                .ToList(),
            ByGrade = rows
                .GroupBy(r => r.GradeKey ?? GradeNone)
                .Select(g => new NameCount { Name = g.Key, Count = g.Count() })
                .OrderByDescending(n => n.Count)
                .ToList()
        };

        await PopulateReportBagsAsync(categoryId: categoryId, courseId: courseId, year: year, from: from, to: to, search: search);
        ViewBag.TraineeTypeFilter = traineeType;

        if (!string.IsNullOrWhiteSpace(export))
            return ToExport(export, _t["Reports.TraineesReport"].Value,
                Col("Reports.Trainee", "Reports.TraineeType", "Reports.AvgScore", "Reports.Grade"),
                model.Rows.Select(r => new object?[]
                {
                    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? r.TraineeNameEn : r.TraineeNameAr,
                    r.TraineeType == TraineeType.Private ? _t["Trainees.PrivateSector"].Value : _t["Trainees.Government"].Value,
                    r.HasGrade ? r.ScorePct : (object?)_t["Reports.NotGraded"].Value,
                    r.HasGrade ? GradeText(r.GradeKey ?? GradeNone) : _t["Reports.NotGraded"].Value
                }).ToList(),
                "TraineeReport");

        return View(model);
    }
}