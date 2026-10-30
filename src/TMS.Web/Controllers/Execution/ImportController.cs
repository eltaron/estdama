using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Identity;
using TMS.Core.Entities.Trainees;
using TMS.Core.Enums;
using TMS.Core.Interfaces;
using TMS.Infrastructure.Services;

namespace TMS.Web.Controllers;

[Authorize(Roles = "Admin,Supervisor")]
public class ImportController : Controller
{
    private readonly IExcelImportService _excelImportService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public ImportController(
        IExcelImportService excelImportService,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork)
    {
        _excelImportService = excelImportService;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult DownloadTemplate()
    {
        var bytes = _excelImportService.GenerateEmployeeTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeImportTemplate.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "يرجى اختيار ملف Excel";
            return RedirectToAction(nameof(Index));
        }

        using var stream = file.OpenReadStream();

        var result = await _excelImportService.ImportEmployees(stream, async draft =>
        {
            if (string.IsNullOrEmpty(draft.Email) || string.IsNullOrEmpty(draft.EmployeeNumber))
                return null;

            if (await _userManager.FindByEmailAsync(draft.Email) != null)
                return null;

            if (await _userManager.Users.AnyAsync(u => u.EmployeeNumber == draft.EmployeeNumber))
                return null;

            var password = GenerateDefaultPassword(draft.EmployeeNumber);

            var user = new ApplicationUser
            {
                UserName = draft.Email,
                Email = draft.Email,
                FullNameAr = draft.FullNameAr,
                FullNameEn = string.IsNullOrEmpty(draft.FullNameEn) ? draft.FullNameAr : draft.FullNameEn,
                EmployeeNumber = draft.EmployeeNumber,
                JobTitle = draft.JobTitle,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
                return null;

            await _userManager.AddToRoleAsync(user, "Trainee");

            var trainee = new Trainee
            {
                UserId = user.Id,
                Type = TraineeType.Government,
                JobTitle = draft.JobTitle
            };
            await _unitOfWork.Repository<Trainee>().AddAsync(trainee);
            await _unitOfWork.SaveChangesAsync();

            return user;
        });

        TempData["Success"] = $"تم استيراد {result.Imported} موظف، تم تخطي {result.Skipped}";
        if (result.Errors.Any())
            TempData["Warnings"] = string.Join(" | ", result.Errors.Take(5));

        return RedirectToAction(nameof(Index));
    }

    private static string GenerateDefaultPassword(string employeeNumber)
    {
        return $"Tms@{employeeNumber}";
    }
}
