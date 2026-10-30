using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
public class CertificatesController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICertificateService _certificateService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<Shared> _localizer;

    public CertificatesController(
        IUnitOfWork unitOfWork,
        ICertificateService certificateService,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<Shared> localizer)
    {
        _unitOfWork = unitOfWork;
        _certificateService = certificateService;
        _userManager = userManager;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var certs = await _unitOfWork.Repository<Certificate>().Query()
            .Include(c => c.Trainee)
                .ThenInclude(t => t.User)
            .Include(c => c.CourseSchedule)
                .ThenInclude(s => s.Course)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync();

        return View(certs);
    }

    public async Task<IActionResult> Details(int id)
    {
        var cert = await _unitOfWork.Repository<Certificate>().Query()
            .Include(c => c.Trainee)
                .ThenInclude(t => t.User)
            .Include(c => c.CourseSchedule)
                .ThenInclude(s => s.Course)
            .Include(c => c.IssuedBy)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cert == null) return NotFound();
        return View(cert);
    }

    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new CertificateFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> Create(CertificateFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var trainee = await _unitOfWork.Repository<Trainee>().GetByIdAsync(model.TraineeId!.Value);
        var schedule = await _unitOfWork.Repository<CourseSchedule>().GetByIdAsync(model.CourseScheduleId!.Value);
        if (trainee == null || schedule == null)
        {
            TempData["Error"] = _localizer["Certificates.TraineeNotFound"].Value;
            await PopulateDropdownsAsync();
            return View(model);
        }

        var duplicate = await _unitOfWork.Repository<Certificate>().FirstOrDefaultAsync(
            c => c.TraineeId == model.TraineeId && c.CourseScheduleId == model.CourseScheduleId && !c.IsRevoked);
        if (duplicate != null)
        {
            TempData["Error"] = _localizer["Certificates.DuplicateError"].Value;
            await PopulateDropdownsAsync();
            return View(model);
        }

        var year = model.IssueDate.Year;
        var yearCount = await _unitOfWork.Repository<Certificate>().CountAsync(c => c.IssueDate.Year == year);
        var serial = $"CERT-{year}-{(yearCount + 1):D4}";

        var cert = new Certificate
        {
            SerialNumber = serial,
            TraineeId = model.TraineeId.Value,
            CourseScheduleId = model.CourseScheduleId.Value,
            IssueDate = model.IssueDate,
            ExpiryDate = model.ExpiryDate,
            IssuedById = _userManager.GetUserId(User)
        };

        await _unitOfWork.Repository<Certificate>().AddAsync(cert);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = _localizer["Certificates.IssuedSuccess"].Value;
        return RedirectToAction(nameof(Details), new { id = cert.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> Revoke(int id, string? reason)
    {
        var cert = await _unitOfWork.Repository<Certificate>().GetByIdAsync(id);
        if (cert == null) return NotFound();

        cert.IsRevoked = true;
        cert.RevocationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        _unitOfWork.Repository<Certificate>().Update(cert);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = _localizer["Certificates.RevokedSuccess"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> DownloadPdf(int id)
    {
        var cert = await _unitOfWork.Repository<Certificate>().Query()
            .Include(c => c.Trainee)
                .ThenInclude(t => t.User)
            .Include(c => c.CourseSchedule)
                .ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cert == null) return NotFound();

        var trainee = cert.Trainee.User;
        var courseName = cert.CourseSchedule.Course.NameAr ?? cert.CourseSchedule.Course.NameEn;
        var durationHours = cert.CourseSchedule.Course.Hours.ToString();

        var data = new CertificateData
        {
            SerialNumber = cert.SerialNumber,
            TraineeNameAr = trainee.FullNameAr ?? trainee.FullNameEn,
            CourseNameAr = courseName,
            IssueDate = cert.IssueDate,
            DurationHours = durationHours
        };

        var pdf = _certificateService.GenerateCertificatePdf(data);
        return File(pdf, "application/pdf", $"Certificate-{cert.SerialNumber}.pdf");
    }

    private async Task PopulateDropdownsAsync()
    {
        var trainees = await _unitOfWork.Repository<Trainee>().Query()
            .Include(t => t.User)
            .Where(t => t.IsActive)
            .OrderBy(t => t.User.FullNameAr)
            .ToListAsync();

        ViewBag.Trainees = new SelectList(trainees.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = $"{t.User.FullNameAr} ({t.User.FullNameEn})"
        }), "Value", "Text");

        var cutoff = DateTime.UtcNow;
        var schedules = await _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Where(s => s.Status == ScheduleStatus.Completed || s.EndDate < cutoff)
            .OrderByDescending(s => s.EndDate)
            .ToListAsync();

        ViewBag.Schedules = new SelectList(schedules.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = $"{s.Course.NameAr} ({s.Course.Code}) - {s.StartDate:yyyy-MM-dd} / {s.EndDate:yyyy-MM-dd}"
        }), "Value", "Text");
    }
}