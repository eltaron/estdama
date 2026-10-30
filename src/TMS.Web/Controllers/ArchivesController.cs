using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using TMS.Core.Entities.Archive;
using TMS.Core.Entities.Common;
using TMS.Core.Entities.Courses;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Infrastructure.Services;
using TMS.Web.ViewModels.Archive;

namespace TMS.Web.Controllers;

[Authorize(Policy = "archive.view")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ArchivesController : Controller
{
    private static readonly string[] AllowedExtensions =
        { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };
    private const long MaxFileSize = 20 * 1024 * 1024;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDatabaseBackupService _backupService;
    private readonly IStringLocalizer<Shared> _t;

    public ArchivesController(IUnitOfWork unitOfWork, IWebHostEnvironment env,
        UserManager<ApplicationUser> userManager, IDatabaseBackupService backupService,
        IStringLocalizer<Shared> t)
    {
        _unitOfWork = unitOfWork;
        _env = env;
        _userManager = userManager;
        _backupService = backupService;
        _t = t;
    }

    private string BackupDirectory => Path.Combine(_env.ContentRootPath, "App_Data", "Backups");

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalDocuments = await _unitOfWork.Repository<ArchiveDocument>().CountAsync();
        ViewBag.ThisMonthDocuments = await _unitOfWork.Repository<ArchiveDocument>().CountAsync(d =>
            d.DocumentDate.Year == DateTime.Today.Year && d.DocumentDate.Month == DateTime.Today.Month);
        ViewBag.ThisYearDocuments = await _unitOfWork.Repository<ArchiveDocument>().CountAsync(d =>
            d.DocumentDate.Year == DateTime.Today.Year);
        ViewBag.IncomingCount = await CountByTypeAsync(ArchiveDocumentType.Incoming);
        ViewBag.OutgoingCount = await CountByTypeAsync(ArchiveDocumentType.Outgoing);
        ViewBag.MemoCount = await CountByTypeAsync(ArchiveDocumentType.InternalMemo);

        var months = Enumerable.Range(0, 6)
            .Select(i => DateTime.Today.AddMonths(i - 5))
            .ToList();

        ViewBag.MonthlyLabels = months.Select(m => m.ToString("MMM")).ToArray();

        var monthlyCounts = new List<int>();
        foreach (var m in months)
        {
            var start = new DateTime(m.Year, m.Month, 1);
            var end = start.AddMonths(1);
            monthlyCounts.Add(await _unitOfWork.Repository<ArchiveDocument>().CountAsync(d =>
                d.DocumentDate >= start && d.DocumentDate < end));
        }
        ViewBag.MonthlyCounts = monthlyCounts;

        var decades = await _unitOfWork.Repository<ArchiveDocument>().Query()
            .Include(d => d.Department)
            .OrderByDescending(d => d.Year)
            .ThenByDescending(d => d.Sequence)
            .Take(6)
            .ToListAsync();
        ViewBag.RecentDocuments = decades;

        return View();
    }

    public async Task<IActionResult> Index(string? code, string? keyword, int? year,
        ArchiveDocumentType? docType, DateTime? fromDate, DateTime? toDate)
    {
        IQueryable<ArchiveDocument> query = _unitOfWork.Repository<ArchiveDocument>().Query()
            .Include(d => d.Department);

        if (!string.IsNullOrWhiteSpace(code))
        {
            var c = code.Trim();
            query = query.Where(d => d.Code.Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(d => d.TitleAr.Contains(k)
                || (d.Subject != null && d.Subject.Contains(k))
                || (d.SenderName != null && d.SenderName.Contains(k))
                || (d.RecipientName != null && d.RecipientName.Contains(k)));
        }

        if (year.HasValue)
            query = query.Where(d => d.Year == year.Value);

        if (docType.HasValue)
            query = query.Where(d => d.DocType == docType.Value);

        if (fromDate.HasValue)
            query = query.Where(d => d.DocumentDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.DocumentDate <= toDate.Value);

        var documents = await query
            .OrderByDescending(d => d.Year)
            .ThenByDescending(d => d.Sequence)
            .Take(500)
            .ToListAsync();

        ViewBag.Code = code;
        ViewBag.Keyword = keyword;
        ViewBag.Year = year;
        ViewBag.DocType = docType;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.AvailableYears = await _unitOfWork.Repository<ArchiveDocument>().Query()
            .Select(d => d.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        return View(documents);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDepartmentsAsync(null);
        return View(new ArchiveDocumentFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "archive.create")]
    public async Task<IActionResult> Create(ArchiveDocumentFormViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
            ModelState.AddModelError("File", "ارفع ملف المستند");

        if (ModelState.IsValid)
        {
            var ext = Path.GetExtension(model.File!.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("File", "نوع الملف غير مسموح به (PDF أو صور أو مستندات مكتبية)");
            }
            else if (model.File.Length > MaxFileSize)
            {
                ModelState.AddModelError("File", "حجم الملف يتجاوز 20 ميجابايت");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsAsync(model.DepartmentId);
            return View(model);
        }

        var year = model.DocumentDate.Year;
        var last = await _unitOfWork.Repository<ArchiveDocument>().Query()
            .Where(d => d.Year == year)
            .OrderByDescending(d => d.Sequence)
            .FirstOrDefaultAsync();
        var sequence = (last?.Sequence ?? 0) + 1;

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "archive", year.ToString());
        Directory.CreateDirectory(uploadDir);

        var ext2 = Path.GetExtension(model.File!.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{ext2}";
        var fullPath = Path.Combine(uploadDir, storedFileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await model.File.CopyToAsync(stream);
        }

        var document = new ArchiveDocument
        {
            Code = $"{year}/{sequence:D5}",
            Year = year,
            Sequence = sequence,
            DocType = model.DocType,
            TitleAr = model.TitleAr.Trim(),
            Subject = model.Subject?.Trim(),
            DocumentDate = model.DocumentDate,
            SenderName = model.SenderName?.Trim(),
            RecipientName = model.RecipientName?.Trim(),
            Notes = model.Notes?.Trim(),
            DepartmentId = model.DepartmentId,
            FilePath = Path.Combine("uploads", "archive", year.ToString(), storedFileName).Replace('\\', '/'),
            OriginalFileName = Path.GetFileName(model.File.FileName),
            ContentType = model.File.ContentType,
            FileSize = model.File.Length,
            CreatedById = _userManager.GetUserId(User)
        };

        await _unitOfWork.Repository<ArchiveDocument>().AddAsync(document);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = $"تمت أرشفة المستند برقم {document.Code}";
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var document = await _unitOfWork.Repository<ArchiveDocument>().Query()
            .Include(d => d.Department)
            .Include(d => d.CreatedBy)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();
        return View(document);
    }

    public async Task<IActionResult> Download(int id)
    {
        var document = await _unitOfWork.Repository<ArchiveDocument>().GetByIdAsync(id);
        if (document == null) return NotFound();

        if (string.IsNullOrWhiteSpace(document.FilePath))
        {
            TempData["Error"] = "لا يوجد ملف مرتبط بهذا المستند";
            return RedirectToAction(nameof(Details), new { id });
        }

        var fullPath = Path.Combine(_env.WebRootPath, document.FilePath);
        if (!System.IO.File.Exists(fullPath))
        {
            TempData["Error"] = "ملف المستند غير موجود على الخادم";
            return RedirectToAction(nameof(Details), new { id });
        }

        var contentType = string.IsNullOrWhiteSpace(document.ContentType)
            ? "application/octet-stream"
            : document.ContentType;
        return PhysicalFile(fullPath, contentType, document.OriginalFileName ?? document.Code);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "archive.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _unitOfWork.Repository<ArchiveDocument>().GetByIdAsync(id);
        if (document == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(document.FilePath))
        {
            var fullPath = Path.Combine(_env.WebRootPath, document.FilePath);
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        _unitOfWork.Repository<ArchiveDocument>().Delete(document);
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "تم حذف المستند من الأرشيف";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> RecordsArchive(int? year)
    {
        IQueryable<CourseSchedule> query = _unitOfWork.Repository<CourseSchedule>().Query()
            .Include(s => s.Course)
            .Include(s => s.Instructor)
            .AsNoTracking()
            .Where(s => s.Status == ScheduleStatus.Completed);

        if (year.HasValue)
            query = query.Where(s => s.StartDate.Year == year.Value);

        var schedules = await query
            .OrderByDescending(s => s.EndDate)
            .ToListAsync();

        var scheduleIds = schedules.Select(s => s.Id).ToList();
        var registrations = await _unitOfWork.Repository<Registration>().Query()
            .Where(r => scheduleIds.Contains(r.CourseScheduleId))
            .GroupBy(r => r.CourseScheduleId)
            .Select(g => new
            {
                ScheduleId = g.Key,
                Count = g.Count(),
                Completed = g.Count(x => x.Status == RegistrationStatus.Completed),
                Collected = g.Sum(x => (decimal?)(x.AmountPaid ?? 0))
            })
            .ToListAsync();

        var bySchedule = registrations.ToDictionary(r => r.ScheduleId);

        var model = new ArchiveRecordsViewModel
        {
            Year = year,
            Schedules = schedules.Select(s =>
            {
                bySchedule.TryGetValue(s.Id, out var reg);
                return new ArchivedScheduleItem
                {
                    ScheduleId = s.Id,
                    CourseCode = s.Course.Code,
                    CourseNameAr = s.Course.NameAr,
                    CourseNameEn = s.Course.NameEn,
                    InstructorName = s.Instructor.FirstNameAr + " " + s.Instructor.LastNameAr,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    RegistrationCount = reg?.Count ?? 0,
                    CompletedCount = reg?.Completed ?? 0,
                    Collected = reg?.Collected ?? 0
                };
            }).ToList(),
            TotalSchedules = schedules.Count,
            TotalRegistrations = registrations.Sum(r => r.Count),
            TotalCompleted = registrations.Sum(r => r.Completed),
            TotalCollected = registrations.Sum(r => r.Collected ?? 0),
            AvailableYears = await _unitOfWork.Repository<CourseSchedule>().Query()
                .Where(s => s.Status == ScheduleStatus.Completed)
                .Select(s => s.StartDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Backup()
    {
        var dir = BackupDirectory;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        ViewBag.BackupFiles = Directory.GetFiles(dir, "*.bak")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "archive.delete")]
    public IActionResult CreateBackup()
    {
        try
        {
            var fileName = _backupService.CreateBackup(BackupDirectory);
            TempData["Success"] = string.Format(_t["ArchiveBackup.Created"].Value, fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = string.Format(_t["ArchiveBackup.CreatedFailed"].Value, ex.Message);
        }
        return RedirectToAction(nameof(Backup));
    }

    public IActionResult DownloadBackup(string file)
    {
        var name = Path.GetFileName(file) ?? string.Empty;
        var fullPath = Path.Combine(BackupDirectory, name);
        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        return PhysicalFile(fullPath, "application/octet-stream", name);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "archive.delete")]
    public IActionResult DeleteBackup(string file)
    {
        var name = Path.GetFileName(file) ?? string.Empty;
        var fullPath = Path.Combine(BackupDirectory, name);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
            TempData["Success"] = _t["ArchiveBackup.Deleted"].Value;
        }
        return RedirectToAction(nameof(Backup));
    }

    public IActionResult Restore()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "archive.delete")]
    public async Task<IActionResult> Restore(IFormFile backupFile)
    {
        if (backupFile == null || backupFile.Length == 0 ||
            !string.Equals(Path.GetExtension(backupFile.FileName), ".bak", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = _t["ArchiveRestore.InvalidFile"].Value;
            return View();
        }

        Directory.CreateDirectory(BackupDirectory);
        var target = Path.Combine(BackupDirectory, $"__restore_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        await using (var stream = System.IO.File.Create(target))
        {
            await backupFile.CopyToAsync(stream);
        }

        try
        {
            _backupService.RestoreFromFile(target);
            TempData["Success"] = _t["ArchiveRestore.Success"].Value;
        }
        catch (Exception ex)
        {
            TempData["Error"] = string.Format(_t["ArchiveRestore.Failed"].Value, ex.Message);
        }
        finally
        {
            if (System.IO.File.Exists(target))
                System.IO.File.Delete(target);
        }
        return RedirectToAction(nameof(Restore));
    }

    private async Task<int> CountByTypeAsync(ArchiveDocumentType type)
    {
        return await _unitOfWork.Repository<ArchiveDocument>().CountAsync(d => d.DocType == type);
    }

    private async Task PopulateDepartmentsAsync(int? selectedId)
    {
        var departments = await _unitOfWork.Repository<Department>().Query()
            .Where(d => d.IsActive)
            .OrderBy(d => d.NameAr)
            .ToListAsync();
        ViewBag.Departments = new SelectList(departments, "Id", "NameAr", selectedId);
    }
}