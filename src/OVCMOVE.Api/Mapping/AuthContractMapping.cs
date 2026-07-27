using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Application.Features.Auth.Command.Login;
using OVCMOVE.Application.Features.Auth.Command.GoogleLogin;
using OVCMOVE.Application.Features.Auth.Query.GetMe;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Api.Mapping;

public static class AuthContractMapping
{
    /// <summary>Maps a login API contract to its application command.</summary>
    public static LoginCommand ToCommand(this AuthContract.LoginRequest request) =>
        new(request.Username, request.Password);

    /// <summary>Maps a Google login API contract to its application command.</summary>
    public static GoogleLoginCommand ToCommand(
        this AuthContract.GoogleLoginRequest request) =>
        new(request.IdToken);

    /// <summary>Maps the login use-case result to the public API response.</summary>
    public static AuthContract.LoginResponse ToResponse(
        this LoginResultModel result) => new()
        {
            AccessToken = result.AccessToken,
            AccessTokenExpiration = result.AccessTokenExpiration,
            UserId = result.UserId,
            Roles = result.Roles.Select(ToResponse).ToArray(),
            Permissions = result.Permissions.Select(ToResponse).ToArray(),
            Access = result.Access
        };

    /// <summary>Maps the current-user use-case result to the public API response.</summary>
    public static AuthContract.MeResponse ToResponse(
        this GetMeResult result) => new()
        {
            Id = result.Id,
            Email = result.Email,
            Roles = result.Roles.Select(ToResponse).ToArray(),
            Permissions = result.Permissions.Select(ToResponse).ToArray(),
            Access = result.Access,
            DisplayName = result.DisplayName,
            Status = result.Status
        };

    private static AuthContract.RoleAccessResponse ToResponse(
        RoleAccessModel role) => new()
        {
            Id = role.Id,
            Name = role.Name,
            Code = role.Code,
            Description = role.Description,
            IsSystem = role.IsSystem
        };

    private static AuthContract.PermissionAccessResponse ToResponse(
        PermissionAccessModel permission) => new()
        {
            Id = permission.Id,
            Name = permission.Name,
            Code = permission.Code,
            Description = permission.Description,
            Module = permission.Module,
            Action = permission.Action,
            IsSystem = permission.IsSystem
        };
}
