using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Execution;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Infrastructure.Services;
using TMS.Web.ViewModels.Execution;

namespace TMS.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private const string QrSigningSalt = "Estidama-TMS-2026-QR-SIGN";
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IQRCodeService _qrCodeService;
    private readonly IStringLocalizer<Shared> _t;

    public AttendanceController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager,
        IQRCodeService qrCodeService, IStringLocalizer<Shared> t)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _qrCodeService = qrCodeService;
        _t = t;
    }

    public async Task<IActionResult> Index(int? courseScheduleId)
    {
        var schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Where(s => s.Status == ScheduleStatus.Active || s.Status == ScheduleStatus.Scheduled)
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
            courseScheduleId);
        ViewBag.SelectedScheduleId = courseScheduleId;

        List<Attendance> records = new();
        if (courseScheduleId.HasValue)
        {
            records = await _unitOfWork.Repository<Attendance>().Query()
                .Include(a => a.CourseSchedule)
                    .ThenInclude(s => s.Course)
                .Include(a => a.Trainee)
                    .ThenInclude(t => t.User)
                .Where(a => a.CourseScheduleId == courseScheduleId.Value)
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        return View(records);
    }

    public async Task<IActionResult> Take(int? courseScheduleId, DateTime? date)
    {
        if (!courseScheduleId.HasValue)
        {
            TempData["Error"] = "اختر دورة أولاً من صفحة الحضور";
            return RedirectToAction(nameof(Index));
        }

        var schedule = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == courseScheduleId.Value);

        if (schedule == null) return NotFound();

        var targetDate = (date ?? DateTime.Today).Date;

        var registrations = await _unitOfWork.Repository<Registration>().Query()
            .Include(r => r.Trainee)
                .ThenInclude(t => t.User)
            .Where(r => r.CourseScheduleId == courseScheduleId.Value &&
                (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Confirmed))
            .OrderBy(r => r.Trainee.User.FullNameAr)
            .ToListAsync();

        var existing = await _unitOfWork.Repository<Attendance>().Query()
            .Where(a => a.CourseScheduleId == courseScheduleId.Value && a.Date.Date == targetDate)
            .ToListAsync();

        var existingDict = existing.ToDictionary(a => a.TraineeId);

        var vm = new TakeAttendanceViewModel
        {
            CourseScheduleId = courseScheduleId.Value,
            CourseName = schedule.Course.NameAr,
            Date = targetDate,
            Rows = registrations.Select(r => new AttendanceRow
            {
                TraineeId = r.TraineeId,
                TraineeName = r.Trainee.User.FullNameAr,
                EmployeeNumber = r.Trainee.EmployeeNumber,
                ExistingStatus = existingDict.TryGetValue(r.TraineeId, out var e) ? e.Status : null,
                Status = existingDict.TryGetValue(r.TraineeId, out var ex) ? ex.Status : AttendanceStatus.Present
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor,Instructor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(TakeAttendanceViewModel vm)
    {
        if (vm.Rows == null || !vm.Rows.Any())
        {
            TempData["Error"] = "لا يوجد متدربون مسجلون في هذا الجدول";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _unitOfWork.Repository<Attendance>().Query()
            .Where(a => a.CourseScheduleId == vm.CourseScheduleId && a.Date.Date == vm.Date.Date)
            .ToListAsync();

        var existingDict = existing.ToDictionary(a => a.TraineeId);
        var recordedById = _userManager.GetUserId(User);
        int saved = 0;

        foreach (var row in vm.Rows)
        {
            if (row.Status == AttendanceStatus.Absent && string.IsNullOrEmpty(row.Notes))
                row.Notes = "غائب";

            if (existingDict.TryGetValue(row.TraineeId, out var record))
            {
                record.Status = row.Status;
                record.Notes = row.Notes;
                _unitOfWork.Repository<Attendance>().Update(record);
            }
            else
            {
                await _unitOfWork.Repository<Attendance>().AddAsync(new Attendance
                {
                    CourseScheduleId = vm.CourseScheduleId,
                    TraineeId = row.TraineeId,
                    Date = vm.Date.Date,
                    Status = row.Status,
                    Notes = row.Notes,
                    CheckInTime = row.Status == AttendanceStatus.Present || row.Status == AttendanceStatus.Late
                        ? DateTime.Now.TimeOfDay
                        : null,
                    RecordedById = recordedById
                });
            }
            saved++;
        }

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = $"تم حفظ الحضور لـ {saved} متدرب بتاريخ {vm.Date:yyyy-MM-dd}";
        return RedirectToAction(nameof(Take), new { courseScheduleId = vm.CourseScheduleId, date = vm.Date.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor,Instructor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(int courseScheduleId, int traineeId, AttendanceStatus status, DateTime date)
    {
        var existing = await _unitOfWork.Repository<Attendance>().FirstOrDefaultAsync(
            a => a.CourseScheduleId == courseScheduleId && a.TraineeId == traineeId && a.Date.Date == date.Date);

        if (existing != null)
        {
            existing.Status = status;
            _unitOfWork.Repository<Attendance>().Update(existing);
        }
        else
        {
            var record = new Attendance
            {
                CourseScheduleId = courseScheduleId,
                TraineeId = traineeId,
                Date = date,
                Status = status,
                RecordedById = _userManager.GetUserId(User)
            };
            await _unitOfWork.Repository<Attendance>().AddAsync(record);
        }

        await _unitOfWork.SaveChangesAsync();
        TempData["Success"] = "تم تسجيل الحضور بنجاح";
        return RedirectToAction(nameof(Index), new { courseScheduleId });
    }

    public IActionResult AddQr(int courseScheduleId, DateTime? date)
    {
        var code = BuildQrPayload(courseScheduleId, (date ?? DateTime.Today).Date);
        var link = Url.Action(nameof(CheckIn), "Attendance", new { code }, Request.Scheme) ?? "";
        return File(_qrCodeService.GenerateQrCode(link), "image/png");
    }

    [AllowAnonymous]
    public async Task<IActionResult> CheckIn(string code)
    {
        var parsed = ReadQrPayload(code);
        if (parsed == null)
        {
            return View(new CheckInViewModel { Message = _t["Attendance.CheckInInvalid"].Value });
        }

        var (scheduleId, date) = parsed.Value;
        var schedule = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule == null) return NotFound();

        var presentCount = await _unitOfWork.Repository<Attendance>().Query()
            .CountAsync(a => a.CourseScheduleId == scheduleId && a.Date.Date == date &&
                (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late));

        var vm = new CheckInViewModel
        {
            Code = code,
            CourseScheduleId = scheduleId,
            CourseName = CourseText(schedule.Course),
            Date = date,
            PresentCount = presentCount
        };
        return View(vm);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInViewModel vm)
    {
        var parsed = ReadQrPayload(vm.Code);
        if (parsed == null)
        {
            vm.Message = _t["Attendance.CheckInInvalid"].Value;
            return View(vm);
        }

        var (scheduleId, date) = parsed.Value;
        var schedule = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule == null) return NotFound();

        vm.CourseScheduleId = scheduleId;
        vm.CourseName = CourseText(schedule.Course);
        vm.Date = date;

        var employeeNumber = vm.EmployeeNumber?.Trim();
        if (string.IsNullOrEmpty(employeeNumber))
        {
            vm.Message = _t["Attendance.CheckInRequired"].Value;
            return View(vm);
        }

        var registration = await _unitOfWork.Repository<Registration>().Query()
            .Include(r => r.Trainee)
                .ThenInclude(t => t.User)
            .Where(r => r.CourseScheduleId == scheduleId &&
                (r.Status == RegistrationStatus.Registered || r.Status == RegistrationStatus.Confirmed) &&
                r.Trainee.EmployeeNumber == employeeNumber)
            .FirstOrDefaultAsync();

        if (registration == null)
        {
            vm.Message = _t["Attendance.CheckInNotFound"].Value;
            return View(vm);
        }

        var existing = await _unitOfWork.Repository<Attendance>().Query()
            .FirstOrDefaultAsync(a => a.CourseScheduleId == scheduleId && a.TraineeId == registration.TraineeId && a.Date.Date == date);

        if (existing != null && existing.Status == AttendanceStatus.Present)
        {
            vm.IsSuccess = true;
            vm.AlreadyMarked = true;
            vm.TraineeName = registration.Trainee.User.FullNameAr;
            vm.CheckInTime = existing.CheckInTime;
            return View(vm);
        }

        if (existing != null)
        {
            existing.Status = AttendanceStatus.Present;
            existing.CheckInTime = DateTime.Now.TimeOfDay;
            existing.QrCode = vm.Code;
            existing.Notes = null;
            _unitOfWork.Repository<Attendance>().Update(existing);
        }
        else
        {
            await _unitOfWork.Repository<Attendance>().AddAsync(new Attendance
            {
                CourseScheduleId = scheduleId,
                TraineeId = registration.TraineeId,
                Date = date,
                Status = AttendanceStatus.Present,
                CheckInTime = DateTime.Now.TimeOfDay,
                QrCode = vm.Code,
                Notes = _t["Attendance.CheckInQrNote"].Value
            });
        }

        await _unitOfWork.SaveChangesAsync();

        vm.IsSuccess = true;
        vm.TraineeName = registration.Trainee.User.FullNameAr;
        vm.CheckInTime = DateTime.Now.TimeOfDay;
        vm.PresentCount++;
        return View(vm);
    }

    private static string CourseText(Course c) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" && !string.IsNullOrWhiteSpace(c.NameEn)
            ? c.NameEn : c.NameAr;

    private static byte[] SignPayload(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(QrSigningSalt));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static string BuildQrPayload(int scheduleId, DateTime date)
    {
        var payload = $"{scheduleId}|{date:yyyy-MM-dd}";
        return payload + "|" + Convert.ToHexString(SignPayload(payload));
    }

    private static (int ScheduleId, DateTime Date)? ReadQrPayload(string? code)
    {
        var parts = code?.Split('|');
        if (parts == null || parts.Length != 3) return null;

        var payload = parts[0] + "|" + parts[1];
        byte[] expected;
        try
        {
            expected = Convert.FromHexString(parts[2]);
        }
        catch (FormatException)
        {
            return null;
        }

        var actual = SignPayload(payload);
        if (!CryptographicOperations.FixedTimeEquals(actual, expected)) return null;

        if (!int.TryParse(parts[0], out var scheduleId)) return null;
        if (!DateTime.TryParseExact(parts[1], "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return null;

        return (scheduleId, date);
    }
}