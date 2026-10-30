using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TMS.Core.Entities.Identity;
using TMS.Core.Security;

namespace TMS.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "Supervisor", "Instructor", "Trainee", "DepartmentHead" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role,
                    NameAr = role switch
                    {
                        "Admin" => "مدير النظام",
                        "Supervisor" => "مشرف التدريب",
                        "Instructor" => "مدرب",
                        "Trainee" => "متدرب",
                        "DepartmentHead" => "مدير إدارة",
                        _ => role
                    },
                    Description = role switch
                    {
                        "Admin" => "صلاحيات كاملة على النظام",
                        "Supervisor" => "إنشاء الدورات وتعيين المدربين ومتابعة الحضور",
                        "Instructor" => "تسجيل الحضور وإدخال درجات دورته فقط",
                        "Trainee" => "تسجيل الدخول والتسجيل في الدورات وأداء الاختبارات",
                        "DepartmentHead" => "عرض تقارير موظفيه وترشيحهم للدورات",
                        _ => ""
                    }
                });
            }
        }

        var adminEmail = "admin@tms.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullNameAr = "مدير النظام",
                FullNameEn = "System Admin",
                EmailConfirmed = true,
                IsActive = true,
                PhoneNumberConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        await SeedDefaultPermissionsAsync(roleManager);
    }

    private static async Task SeedDefaultPermissionsAsync(RoleManager<ApplicationRole> roleManager)
    {
        var defaults = new Dictionary<string, string[]>
        {
            ["Supervisor"] = PermissionCatalog.AllPermissions.Where(p =>
                p.StartsWith("users.") || p.StartsWith("courses.") ||
                p.StartsWith("training.") || p.StartsWith("instructors.") ||
                p.StartsWith("accounts.") || p.StartsWith("archive.") ||
                p.StartsWith("reports.")).ToArray(),
            ["DepartmentHead"] = new[] { "training.view", "reports.view" }
        };

        foreach (var pair in defaults)
        {
            var role = await roleManager.FindByNameAsync(pair.Key);
            if (role == null) continue;

            var existing = (await roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == PermissionCatalog.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permission in pair.Value)
            {
                if (!existing.Contains(permission))
                {
                    await roleManager.AddClaimAsync(role,
                        new Claim(PermissionCatalog.ClaimType, permission));
                }
            }
        }
    }
}
