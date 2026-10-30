using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Resources;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Web.ViewModels.Reports;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.ReportsView)]
public class ConflictsController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ConflictsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? from, string? to)
    {
        var fromD = ParseDate(from) ?? DateTime.MinValue;
        var toD = ParseDate(to) ?? DateTime.MaxValue;
        ViewBag.FromFilter = from;
        ViewBag.ToFilter = to;

        var schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .Include(s => s.Room)
            .Where(s => s.Status != ScheduleStatus.Cancelled)
            .OrderBy(s => s.StartDate)
            .ToListAsync();

        var candidates = schedules
            .Where(s => s.StartDate <= toD && s.EndDate >= fromD)
            .ToList();

        var vm = new ConflictListViewModel();

        for (int i = 0; i < candidates.Count; i++)
        {
            for (int j = i + 1; j < candidates.Count; j++)
            {
                var a = candidates[i];
                var b = candidates[j];
                if (!Overlaps(a, b)) continue;

                if (a.InstructorId == b.InstructorId)
                {
                    vm.InstructorConflicts.Add(new ScheduleConflictItem
                    {
                        ResourceName = InstructorName(a.Instructor),
                        CourseAName = CourseName(a.Course),
                        ScheduleATimes = RangeText(a),
                        CourseBName = CourseName(b.Course),
                        ScheduleBTimes = RangeText(b)
                    });
                }

                if (a.RoomId != null && a.RoomId == b.RoomId)
                {
                    vm.RoomConflicts.Add(new ScheduleConflictItem
                    {
                        ResourceName = a.Room != null ? RoomName(a.Room) : "?", 
                        CourseAName = CourseName(a.Course),
                        ScheduleATimes = RangeText(a),
                        CourseBName = CourseName(b.Course),
                        ScheduleBTimes = RangeText(b)
                    });
                }
            }
        }

        var loans = await _unitOfWork.Repository<EquipmentLoan>().Query()
            .Include(l => l.Equipment)
            .Include(l => l.CourseSchedule).ThenInclude(s => s.Course)
            .Where(l => l.Status != LoanStatus.Returned)
            .OrderBy(l => l.LoanDate)
            .ToListAsync();

        var loansInRange = loans
            .Where(l => l.LoanDate <= toD && (l.ReturnDate ?? DateTime.MaxValue) >= fromD)
            .ToList();

        for (int i = 0; i < loansInRange.Count; i++)
        {
            for (int j = i + 1; j < loansInRange.Count; j++)
            {
                var a = loansInRange[i];
                var b = loansInRange[j];
                if (a.EquipmentId != b.EquipmentId) continue;
                var aEnd = a.ReturnDate ?? DateTime.MaxValue;
                var bEnd = b.ReturnDate ?? DateTime.MaxValue;
                if (a.LoanDate <= bEnd && b.LoanDate <= aEnd)
                {
                    vm.EquipmentConflicts.Add(new EquipmentConflictItem
                    {
                        EquipmentName = EquipmentName(a.Equipment),
                        CourseAName = CourseName(a.CourseSchedule.Course),
                        LoanAPeriod = LoanPeriodText(a),
                        CourseBName = CourseName(b.CourseSchedule.Course),
                        LoanBPeriod = LoanPeriodText(b)
                    });
                }
            }
        }

        return View(vm);
    }

    public static bool Overlaps(CourseSchedule a, CourseSchedule b) =>
        a.StartDate <= b.EndDate && b.StartDate <= a.EndDate &&
        a.StartTime < b.EndTime && b.StartTime < a.EndTime;

    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

    private static bool IsEn => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    private string InstructorName(TMS.Core.Entities.Instructors.Instructor i)
    {
        var ar = $"{i.FirstNameAr} {i.LastNameAr}".Trim();
        var en = $"{i.FirstNameEn} {i.LastNameEn}".Trim();
        return IsEn && !string.IsNullOrEmpty(en) ? en : ar;
    }

    private string CourseName(Course c) => IsEn && !string.IsNullOrWhiteSpace(c.NameEn) ? c.NameEn : c.NameAr;

    private string RoomName(Room r) => IsEn && !string.IsNullOrWhiteSpace(r.NameEn) ? r.NameEn : r.NameAr;

    private string EquipmentName(Equipment e) => IsEn && !string.IsNullOrWhiteSpace(e.NameEn) ? e.NameEn : e.NameAr;

    private static string RangeText(CourseSchedule s) =>
        $"{s.StartDate:yyyy-MM-dd} {s.StartTime:hh\\:mm} - {s.EndDate:yyyy-MM-dd} {s.EndTime:hh\\:mm}";

    private static string LoanPeriodText(EquipmentLoan l) =>
        l.ReturnDate.HasValue
            ? $"{l.LoanDate:yyyy-MM-dd} - {l.ReturnDate:yyyy-MM-dd}"
            : l.LoanDate.ToString("yyyy-MM-dd");
}