using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Common;
using TMS.Core.Entities.Identity;
using TMS.Core.Interfaces;
using TMS.Core.Security;
using TMS.Web.ViewModels.Users;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.UsersView)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index(string? search, string? roleId)
    {
        var users = await _userManager.Users
            .Include(u => u.Department)
            .Where(u => string.IsNullOrWhiteSpace(search)
                || u.FullNameAr.Contains(search.Trim())
                || u.FullNameEn.Contains(search.Trim())
                || (u.Email != null && u.Email.Contains(search.Trim()))
                || (u.EmployeeNumber != null && u.EmployeeNumber.Contains(search.Trim())))
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserListViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (!string.IsNullOrWhiteSpace(roleId))
            {
                var roleEntity = await _roleManager.FindByIdAsync(roleId);
                if (roleEntity != null && !roles.Contains(roleEntity.Name!))
                    continue;
            }

            var primaryRole = string.Empty;
            if (roles.Count > 0)
            {
                var primaryRoleEntity = await _roleManager.FindByNameAsync(roles[0]);
                primaryRole = primaryRoleEntity?.NameAr ?? roles[0];
            }

            result.Add(new UserListViewModel
            {
                Id = user.Id,
                FullNameAr = user.FullNameAr,
                FullNameEn = user.FullNameEn,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmployeeNumber = user.EmployeeNumber,
                DepartmentName = user.Department?.NameAr,
                JobTitle = user.JobTitle,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles,
                RoleNameAr = primaryRole
            });
        }

        ViewBag.Search = search;
        ViewBag.Roles = new SelectList(
            await _roleManager.Roles.OrderBy(r => r.NameAr).ToListAsync(),
            "Id", "NameAr", roleId);

        return View(result);
    }

    [Authorize(Policy = PermissionCatalog.UsersCreate)]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync(null);
        return View(new UserFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UsersCreate)]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "ظƒظ„ظ…ط© ط§ظ„ظ…ط±ظˆط± ظ…ط·ظ„ظˆط¨ط© ظ„ط¥ط¶ط§ظپط© ظ…ط³طھط®ط¯ظ…");
        }
        else if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "ظƒظ„ظ…طھط§ ط§ظ„ظ…ط±ظˆط± ط؛ظٹط± ظ…طھط·ط§ط¨ظ‚طھظٹظ†");
        }

        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        if (await _userManager.FindByEmailAsync(model.Email.Trim()) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ظ…ط³طھط®ط¯ظ… ط¨ط§ظ„ظپط¹ظ„");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var role = await _roleManager.FindByIdAsync(model.RoleId);
        if (role == null)
        {
            ModelState.AddModelError(nameof(model.RoleId), "ط§ظ„ط¯ظˆط± ط§ظ„ظ…ط­ط¯ط¯ ط؛ظٹط± ظ…ظˆط¬ظˆط¯");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullNameAr = model.FullNameAr.Trim(),
            FullNameEn = model.FullNameEn.Trim(),
            PhoneNumber = model.PhoneNumber,
            EmployeeNumber = model.EmployeeNumber,
            NationalId = model.NationalId,
            DepartmentId = model.DepartmentId,
            JobTitle = model.JobTitle,
            IsActive = model.IsActive,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password!);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role.Name!);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await _userManager.DeleteAsync(user);
            await PopulateLookupsAsync(model);
            return View(model);
        }

        TempData["Success"] = $"طھظ… ط¥ط¶ط§ظپط© ط§ظ„ظ…ط³طھط®ط¯ظ… {user.FullNameAr} ط¨ظ†ط¬ط§ط­";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionCatalog.UsersEdit)]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? string.Empty;
        var role = roleName != string.Empty ? await _roleManager.FindByNameAsync(roleName) : null;

        var model = new UserFormViewModel
        {
            Id = user.Id,
            FullNameAr = user.FullNameAr,
            FullNameEn = user.FullNameEn,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            EmployeeNumber = user.EmployeeNumber,
            NationalId = user.NationalId,
            DepartmentId = user.DepartmentId,
            JobTitle = user.JobTitle,
            RoleId = role?.Id ?? string.Empty,
            IsActive = user.IsActive
        };

        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UsersEdit)]
    public async Task<IActionResult> Edit(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id!);
        if (user == null) return NotFound();

        if (!string.Equals(model.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase)
            && await _userManager.FindByEmailAsync(model.Email.Trim()) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ظ…ط³طھط®ط¯ظ… ط¨ط§ظ„ظپط¹ظ„");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var role = await _roleManager.FindByIdAsync(model.RoleId);
        if (role == null)
        {
            ModelState.AddModelError(nameof(model.RoleId), "ط§ظ„ط¯ظˆط± ط§ظ„ظ…ط­ط¯ط¯ ط؛ظٹط± ظ…ظˆط¬ظˆط¯");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var isSelf = string.Equals(model.Id, _userManager.GetUserId(User), StringComparison.OrdinalIgnoreCase);
        if (isSelf && !model.IsActive)
        {
            ModelState.AddModelError(nameof(model.IsActive), "ظ„ط§ ظٹظ…ظƒظ†ظƒ طھط¹ط·ظٹظ„ ط­ط³ط§ط¨ظƒ ط¨ظ†ظپط³ظƒ");
            await PopulateLookupsAsync(model);
            return View(model);
        }

        user.FullNameAr = model.FullNameAr.Trim();
        user.FullNameEn = model.FullNameEn.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.EmployeeNumber = model.EmployeeNumber;
        user.NationalId = model.NationalId;
        user.DepartmentId = model.DepartmentId;
        user.JobTitle = model.JobTitle;
        user.IsActive = model.IsActive;

        if (!string.Equals(model.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();
            user.EmailConfirmed = true;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!(currentRoles.Count == 1 && currentRoles[0] == role.Name))
        {
            if (currentRoles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role.Name!);
        }

        TempData["Success"] = $"طھظ… طھط­ط¯ظٹط« ط¨ظٹط§ظ†ط§طھ ط§ظ„ظ…ط³طھط®ط¯ظ… {user.FullNameAr} ط¨ظ†ط¬ط§ط­";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionCatalog.UsersEdit)]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        return View(new ResetPasswordViewModel { Id = user.Id, Email = user.Email ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UsersEdit)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        var hasPassword = await _userManager.HasPasswordAsync(user);
        var removeResult = IdentityResult.Success;
        if (hasPassword)
        {
            removeResult = await _userManager.RemovePasswordAsync(user);
        }

        var addResult = IdentityResult.Failed();
        if (removeResult.Succeeded)
        {
            addResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
        }

        if (addResult.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["Success"] = $"طھظ… ط¥ط¹ط§ط¯ط© طھط¹ظٹظٹظ† ظƒظ„ظ…ط© ظ…ط±ظˆط± ط§ظ„ظ…ط³طھط®ط¯ظ… {user.FullNameAr} ط¨ظ†ط¬ط§ط­";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in addResult.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UsersEdit)]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (string.Equals(id, _userManager.GetUserId(User), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "ظ„ط§ ظٹظ…ظƒظ†ظƒ طھط¹ط·ظٹظ„ ط£ظˆ طھظپط¹ظٹظ„ ط­ط³ط§ط¨ظƒ ط¨ظ†ظپط³ظƒ";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = user.IsActive
            ? $"طھظ… طھظپط¹ظٹظ„ ط§ظ„ظ…ط³طھط®ط¯ظ… {user.FullNameAr}"
            : $"طھظ… طھط¹ط·ظٹظ„ ط§ظ„ظ…ط³طھط®ط¯ظ… {user.FullNameAr}";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(UserFormViewModel? model)
    {
        ViewBag.Roles = new SelectList(
            await _roleManager.Roles.OrderBy(r => r.NameAr).ToListAsync(),
            "Id", "NameAr", model?.RoleId);

        var departments = await _unitOfWork.Repository<Department>().Query()
            .Where(d => d.IsActive)
            .OrderBy(d => d.NameAr)
            .ToListAsync();

        ViewBag.Departments = new SelectList(departments, "Id", "NameAr", model?.DepartmentId);
    }
}