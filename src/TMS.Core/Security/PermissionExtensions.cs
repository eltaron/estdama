using System.Security.Claims;

namespace TMS.Core.Security;

public static class PermissionExtensions
{
    public static string PermissionName(this ClaimsPrincipal? user, string moduleKey, string actionKey)
        => PermissionCatalog.Permission(moduleKey, actionKey);

    public static bool Can(this ClaimsPrincipal user, string moduleKey, string actionKey)
        => user.Can(PermissionCatalog.Permission(moduleKey, actionKey));

    public static bool Can(this ClaimsPrincipal user, string permissionName)
    {
        if (user.IsInRole(PermissionCatalog.AdminRole))
            return true;

        return user.HasClaim(PermissionCatalog.ClaimType, permissionName);
    }
}
