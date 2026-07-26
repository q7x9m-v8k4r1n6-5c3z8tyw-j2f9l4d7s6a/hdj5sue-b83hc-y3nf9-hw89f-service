using MediatR;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Query.GetAllPermissions;

public record GetAllPermissionsQuery() : IRequest<IReadOnlyCollection<PermissionSummaryModel>>;
