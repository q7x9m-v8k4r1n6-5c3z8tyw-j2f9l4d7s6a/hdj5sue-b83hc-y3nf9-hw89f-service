using MediatR;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Auth.Query.GetMe;

public record GetMeResult(
    Guid Id,
    string Email,
    string UserType,
    IReadOnlyCollection<RoleAccessModel> Roles,
    IReadOnlyCollection<PermissionAccessModel> Permissions,
    IReadOnlyCollection<string> Access,
    string? DisplayName,
    string Status);

public record GetMeQuery(Guid UserId) : IRequest<GetMeResult>;
