using Microsoft.AspNetCore.Authorization;
using TMS.Core.Security;

namespace TMS.Web.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionName)
    {
        PermissionName = permissionName;
    }

    public string PermissionName { get; }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole(PermissionCatalog.AdminRole)
            || context.User.HasClaim(PermissionCatalog.ClaimType, requirement.PermissionName))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}