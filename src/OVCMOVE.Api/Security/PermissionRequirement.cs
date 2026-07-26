using Microsoft.AspNetCore.Authorization;

namespace OVCMOVE.Api.Security;

public class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
