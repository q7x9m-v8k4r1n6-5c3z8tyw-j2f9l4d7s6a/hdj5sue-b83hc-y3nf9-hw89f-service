using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace OVCMOVE.Api.Security;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var hasPermission = context.User.Claims.Any(claim =>
            claim.Type == PermissionClaimType &&
            string.Equals(
                claim.Value,
                requirement.PermissionCode,
                StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
