using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Core.Entities.Identity;
using TMS.Core.Security;
using TMS.Web.ViewModels.Roles;

namespace TMS.Web.Controllers;

[Authorize(Policy = PermissionCatalog.RolesView)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RolesController : Controller
{
    private const string SystemRoleName = "Admin";

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

        var result = new List<RoleListViewModel>();
        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role.Name!);
            var claims = await _roleManager.GetClaimsAsync(role);

            result.Add(new RoleListViewModel
            {
                Id = role.Id,
                Name = role.Name!,
                NameAr = role.NameAr,
                Description = role.Description,
                UserCount = users.Count,
                PermissionCount = claims.Count(c => c.Type == PermissionCatalog.ClaimType),
                IsSystemRole = role.Name == SystemRoleName
            });
        }

        return View(result);
    }

    [Authorize(Policy = PermissionCatalog.RolesCreate)]
    public IActionResult Create()
    {
        return View(new RoleFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.RolesCreate)]
    public async Task<IActionResult> Create(RoleFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _roleManager.RoleExistsAsync(model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "ظٹظˆط¬ط¯ ط¯ظˆط± ط¨ظ†ظپط³ ط§ظ„ط§ط³ظ… ط§ظ„ظپظ†ظٹ");
            return View(model);
        }

        var role = new ApplicationRole
        {
            Name = model.Name.Trim(),
            NameAr = model.NameAr.Trim(),
            Description = model.Description?.Trim()
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = $"طھظ… ط¥ظ†ط´ط§ط، ط§ظ„ط¯ظˆط± {role.NameAr} â€” ظ‚ظ… ط¨طھط­ط¯ظٹط¯ طµظ„ط§ط­ظٹط§طھظ‡";
        return RedirectToAction(nameof(Edit), new { id = role.Id });
    }

    [Authorize(Policy = PermissionCatalog.RolesEdit)]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        var claims = await _roleManager.GetClaimsAsync(role);

        var model = new RoleFormViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            NameAr = role.NameAr ?? string.Empty,
            Description = role.Description,
            GrantedPermissions = claims
                .Where(c => c.Type == PermissionCatalog.ClaimType)
                .Select(c => c.Value)
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.RolesEdit)]
    public async Task<IActionResult> Edit(RoleFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var role = await _roleManager.FindByIdAsync(model.Id!);
        if (role == null) return NotFound();

        role.NameAr = model.NameAr.Trim();
        role.Description = model.Description?.Trim();

        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        if (role.Name != SystemRoleName)
        {
            var existingClaims = (await _roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == PermissionCatalog.ClaimType)
                .ToList();

            var existingValues = existingClaims.Select(c => c.Value).ToHashSet();
            var desiredSet = model.GrantedPermissions.ToHashSet();

            foreach (var claim in existingClaims)
            {
                if (!desiredSet.Contains(claim.Value))
                    await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var permission in desiredSet)
            {
                if (!existingValues.Contains(permission))
                    await _roleManager.AddClaimAsync(role,
                        new System.Security.Claims.Claim(PermissionCatalog.ClaimType, permission));
            }

            foreach (var user in await _userManager.GetUsersInRoleAsync(role.Name!))
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }
        }

        TempData["Success"] = $"طھظ… طھط­ط¯ظٹط« ط§ظ„ط¯ظˆط± {role.NameAr} ظˆطµظ„ط§ط­ظٹط§طھظ‡ ط¨ظ†ط¬ط§ط­";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.RolesDelete)]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        if (role.Name == SystemRoleName)
        {
            TempData["Error"] = "ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ط¯ظˆط± ظ…ط¯ظٹط± ط§ظ„ظ†ط¸ط§ظ…";
            return RedirectToAction(nameof(Index));
        }

        var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
        if (userCount > 0)
        {
            TempData["Error"] = "ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ط§ظ„ط¯ظˆط± ظ„ظˆط¬ظˆط¯ ظ…ط³طھط®ط¯ظ…ظٹظ† ظ…ط±طھط¨ط·ظٹظ† ط¨ظ‡";
            return RedirectToAction(nameof(Index));
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Errors.FirstOrDefault()?.Description ?? "طھط¹ط°ط± ط­ط°ظپ ط§ظ„ط¯ظˆط±";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"طھظ… ط­ط°ظپ ط§ظ„ط¯ظˆط± {role.NameAr}";
        return RedirectToAction(nameof(Index));
    }
}