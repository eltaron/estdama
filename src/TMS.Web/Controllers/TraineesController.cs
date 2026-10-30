using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Common;
using TMS.Core.Entities.Execution;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Infrastructure.Services;
using TMS.Web.ViewModels.Trainees;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.TrainingView)]
public class TraineesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITraineeFileService _traineeFileService;

    public TraineesController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, ITraineeFileService traineeFileService)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _traineeFileService = traineeFileService;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .Include(t => t.CurrentDepartment)
            .Where(t => t.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.User.FullNameAr.Contains(search)
                || t.User.NationalId!.Contains(search)
                || (t.Organization != null && t.Organization.Contains(search)));

        var trainees = await query.OrderByDescending(t => t.Id).ToListAsync();

        ViewBag.Search = search;
        return View(trainees);
    }

    public async Task<IActionResult> Details(int id)
    {
        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .Include(t => t.CurrentDepartment)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.CourseSchedule)
                    .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainee == null) return NotFound();
        return View(trainee);
    }

    [Authorize(Policy = PermissionCatalog.TrainingEdit)]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .Include(t => t.CurrentDepartment)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.CourseSchedule)
                    .ThenInclude(s => s.Course)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.CourseSchedule)
                    .ThenInclude(s => s.Instructor)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainee == null) return NotFound();

        var user = trainee.User;

        var courses = trainee.Registrations
            .Where(r => r.CourseSchedule != null)
            .Select(r => new TraineeFileCourse
            {
                CourseCode = r.CourseSchedule!.Course != null ? r.CourseSchedule.Course.Code : "-",
                CourseName = r.CourseSchedule!.Course?.NameAr ?? r.CourseSchedule.Course?.NameEn ?? "-",
                TrainerName = r.CourseSchedule.Instructor != null
                    ? $"{r.CourseSchedule.Instructor.FirstNameAr} {r.CourseSchedule.Instructor.LastNameAr}".Trim()
                    : "-",
                StartDate = r.CourseSchedule.StartDate,
                EndDate = r.CourseSchedule.EndDate,
                Hours = r.CourseSchedule.Course?.Hours ?? 0,
                Status = StatusText(r.Status),
                PaymentStatus = PaymentText(r.PaymentStatus)
            })
            .OrderBy(c => c.StartDate)
            .ToList();

        var scheduleIds = trainee.Registrations
            .Select(r => r.CourseScheduleId)
            .Distinct()
            .ToList();

        var exams = await _unitOfWork.Repository<ExamSubmission>().Query()
            .Include(s => s.Exam)
                .ThenInclude(e => e.CourseSchedule)
                    .ThenInclude(s => s.Course)
            .Where(s => s.TraineeId == trainee.Id && s.Status == SubmissionStatus.Completed)
            .OrderByDescending(s => s.SubmitTime)
            .ToListAsync();

        var examData = exams.Select(s => new TraineeFileExam
        {
            ExamTitle = s.Exam?.TitleAr ?? "ط§ط®طھط¨ط§ط±",
            CourseName = s.Exam?.CourseSchedule?.Course?.NameAr ?? "-",
            TotalMarks = s.Exam?.TotalMarks ?? 0,
            Score = s.TotalScore,
            PassingMarks = s.Exam?.PassingMarks ?? 0,
            IsPassed = s.IsPassed,
            SubmittedAt = s.SubmitTime ?? s.StartTime,
            Status = s.Status.ToString()
        }).ToList();

        var data = new TraineeFileData
        {
            FullNameAr = user.FullNameAr,
            FullNameEn = user.FullNameEn,
            NationalId = user.NationalId ?? "-",
            TraineeType = trainee.Type == TraineeType.Private ? "ظ‚ط·ط§ط¹ ط®ط§طµ" : "ط­ظƒظˆظ…ظٹ",
            Email = user.Email ?? "-",
            Phone = trainee.Phone ?? "-",
            Address = trainee.Address ?? "-",
            JobTitle = trainee.JobTitle ?? "-",
            Organization = trainee.Organization ?? "-",
            Directorate = trainee.Directorate ?? "-",
            JobGrade = trainee.JobGrade ?? "-",
            GeneratedAt = DateTime.Now,
            Courses = courses,
            Exams = examData
        };

        var pdf = _traineeFileService.GenerateTraineeFilePdf(data);
        var fileName = $"TraineeFile-{trainee.Id}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    private static string StatusText(RegistrationStatus status) => status switch
    {
        RegistrationStatus.Confirmed => "ظ…ط¤ظƒط¯",
        RegistrationStatus.Completed => "ظ…ظƒطھظ…ظ„",
        RegistrationStatus.Dropped => "ظ…ظ†ط³ط­ط¨",
        RegistrationStatus.Waitlisted => "ظ‚ط§ط¦ظ…ط© ط§ظ†طھط¸ط§ط±",
        _ => "ظ…ط³ط¬ظ„"
    };

    private static string PaymentText(PaymentStatus status) => status switch
    {
        PaymentStatus.Approved => "ظ…ط¹طھظ…ط¯",
        PaymentStatus.Paid => "ظ…ط¯ظپظˆط¹",
        PaymentStatus.Partial => "ط¬ط²ط¦ظٹ",
        _ => "ط؛ظٹط± ظ…ط¯ظپظˆط¹"
    };

    [Authorize(Policy = PermissionCatalog.TrainingCreate)]
    public async Task<IActionResult> Create()
    {
        await PopulateDepartmentsAsync();
        return View(new TraineeFormViewModel());
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.TrainingCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TraineeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsAsync();
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.Email) && await _userManager.FindByEmailAsync(model.Email!) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "ظ‡ط°ط§ ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ظ…ط³طھط®ط¯ظ… ط¨ط§ظ„ظپط¹ظ„");
            await PopulateDepartmentsAsync();
            return View(model);
        }

        var email = string.IsNullOrEmpty(model.Email)
            ? $"trainee{Guid.NewGuid():N}@tms.local"
            : model.Email!;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = model.Email,
            FullNameAr = model.FullNameAr,
            FullNameEn = string.IsNullOrEmpty(model.FullNameEn) ? model.FullNameAr : model.FullNameEn,
            NationalId = model.NationalId,
            JobTitle = model.JobTitle,
            IsActive = true,
            EmailConfirmed = true
        };

        var password = $"Tms@{model.NationalId ?? "12345"}";
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            await PopulateDepartmentsAsync();
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Trainee");

        var trainee = new Trainee
        {
            UserId = user.Id,
            Type = model.Type,
            Phone = model.Phone,
            Address = model.Address,
            JobTitle = model.Type == TraineeType.Government ? model.JobTitle : null,
            Organization = model.Type == TraineeType.Government ? model.Organization : null,
            Directorate = model.Type == TraineeType.Government ? model.Directorate : null,
            JobGrade = model.Type == TraineeType.Government ? model.JobGrade : null,
            Notes = model.Notes,
            IsActive = true
        };

        await _unitOfWork.Repository<Trainee>().AddAsync(trainee);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "طھظ… ط¥ط¶ط§ظپط© ط§ظ„ظ…طھط¯ط±ط¨ ط¨ظ†ط¬ط§ط­";
        return RedirectToAction(nameof(Details), new { id = trainee.Id });
    }

    [Authorize(Roles = "Trainee")]
    public async Task<IActionResult> MySchedule()
    {
        var userId = _userManager.GetUserId(User);
        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.CourseSchedule)
                    .ThenInclude(s => s.Course)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.CourseSchedule)
                    .ThenInclude(s => s.Instructor)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        return View(trainee);
    }

    [Authorize(Roles = "Trainee")]
    public async Task<IActionResult> MyCertificates()
    {
        var userId = _userManager.GetUserId(User);
        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (trainee == null) return View(new List<TMS.Core.Entities.Execution.Certificate>());

        var certificates = await _unitOfWork.Repository<TMS.Core.Entities.Execution.Certificate>().Query()
            .Include(c => c.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Where(c => c.TraineeId == trainee.Id)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync();

        return View(certificates);
    }

    [Authorize(Roles = "Trainee")]
    public async Task<IActionResult> MyTrainingReport()
    {
        var userId = _userManager.GetUserId(User);
        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.CourseSchedule)
                    .ThenInclude(s => s.Course)
            .Include(t => t.Registrations)
                .ThenInclude(r => r.CourseSchedule)
                    .ThenInclude(s => s.Instructor)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (trainee == null)
        {
            ViewBag.TraineeName = User.Identity?.Name;
            ViewBag.CompletedCourses = new List<TraineeFileCourse>();
            return View();
        }

        var completed = trainee.Registrations
            .Where(r => r.Status == RegistrationStatus.Completed && r.CourseSchedule != null)
            .Select(r => new TraineeFileCourse
            {
                CourseCode = r.CourseSchedule!.Course?.Code ?? "-",
                CourseName = r.CourseSchedule!.Course?.NameAr ?? r.CourseSchedule.Course?.NameEn ?? "-",
                TrainerName = r.CourseSchedule.Instructor != null
                    ? $"{r.CourseSchedule.Instructor.FirstNameAr} {r.CourseSchedule.Instructor.LastNameAr}".Trim()
                    : "-",
                StartDate = r.CourseSchedule.StartDate,
                EndDate = r.CourseSchedule.EndDate,
                Hours = r.CourseSchedule.Course?.Hours ?? 0,
                Status = StatusText(r.Status),
                PaymentStatus = PaymentText(r.PaymentStatus)
            })
            .OrderBy(c => c.StartDate)
            .ToList();

        var fullName = trainee.User.FullNameAr ?? User.Identity?.Name ?? "";
        ViewBag.TraineeName = fullName;
        ViewBag.NationalId = trainee.User.NationalId;
        ViewBag.Organization = trainee.Organization;
        ViewBag.Directorate = trainee.Directorate;
        ViewBag.JobTitle = trainee.JobTitle;
        ViewBag.CompletedCourses = completed;
        ViewBag.TotalCourses = completed.Count;
        ViewBag.TotalHours = completed.Sum(c => c.Hours);
        return View();
    }

    [Authorize(Policy = PermissionCatalog.TrainingEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trainee == null) return NotFound();

        var model = new TraineeFormViewModel
        {
            Id = trainee.Id,
            UserId = trainee.UserId,
            Type = trainee.Type,
            FullNameAr = trainee.User.FullNameAr,
            FullNameEn = trainee.User.FullNameEn,
            Email = trainee.User.Email,
            NationalId = trainee.User.NationalId,
            Phone = trainee.Phone,
            Address = trainee.Address,
            JobTitle = trainee.JobTitle,
            Organization = trainee.Organization,
            Directorate = trainee.Directorate,
            JobGrade = trainee.JobGrade,
            Notes = trainee.Notes
        };

        await PopulateDepartmentsAsync();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.TrainingEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TraineeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsAsync();
            return View(model);
        }

        var trainee = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == model.Id);

        if (trainee == null) return NotFound();

        var user = trainee.User;
        user.FullNameAr = model.FullNameAr;
        user.FullNameEn = string.IsNullOrEmpty(model.FullNameEn) ? model.FullNameAr : model.FullNameEn;
        user.NationalId = model.NationalId;
        user.JobTitle = model.Type == TraineeType.Government ? model.JobTitle : null;

        if (!string.IsNullOrEmpty(model.Email) && model.Email != user.Email)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "ظ‡ط°ط§ ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ظ…ط³طھط®ط¯ظ… ط¨ط§ظ„ظپط¹ظ„");
                await PopulateDepartmentsAsync();
                return View(model);
            }
            user.Email = model.Email;
            user.UserName = model.Email;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            await PopulateDepartmentsAsync();
            return View(model);
        }

        trainee.Type = model.Type;
        trainee.Phone = model.Phone;
        trainee.Address = model.Address;
        trainee.JobTitle = model.Type == TraineeType.Government ? model.JobTitle : null;
        trainee.Organization = model.Type == TraineeType.Government ? model.Organization : null;
        trainee.Directorate = model.Type == TraineeType.Government ? model.Directorate : null;
        trainee.JobGrade = model.Type == TraineeType.Government ? model.JobGrade : null;
        trainee.Notes = model.Notes;

        _unitOfWork.Repository<Trainee>().Update(trainee);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "طھظ… طھط­ط¯ظٹط« ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…طھط¯ط±ط¨ ط¨ظ†ط¬ط§ط­";
        return RedirectToAction(nameof(Details), new { id = trainee.Id });
    }

    private async Task PopulateDepartmentsAsync()
    {
        var departments = await _unitOfWork.Repository<Department>()
            .Query()
            .Where(d => d.IsActive)
            .OrderBy(d => d.NameAr)
            .ToListAsync();

        ViewBag.Departments = new SelectList(departments, "Id", "NameAr");
    }
}