using Microsoft.AspNetCore.Authorization;

namespace OVCMOVE.Api.Security;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        Policy = $"{PermissionAuthorizationPolicyProvider.PolicyPrefix}{permissionCode}";
    }
}
