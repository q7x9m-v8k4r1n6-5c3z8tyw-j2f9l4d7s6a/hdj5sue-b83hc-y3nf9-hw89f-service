using System;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.DTOs.ResultModels;

public class LoginResultModel
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiration { get; init; }
    public DateTime RefreshTokenExpiration { get; init; }
    public Guid UserId { get; init; }
    public string UserType { get; init; } = string.Empty;
    public IReadOnlyCollection<RoleAccessModel> Roles { get; init; } = Array.Empty<RoleAccessModel>();
    public IReadOnlyCollection<PermissionAccessModel> Permissions { get; init; } = Array.Empty<PermissionAccessModel>();
    public IReadOnlyCollection<string> Access { get; init; } = Array.Empty<string>();
}
