using MediatR;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Roles.Query.GetAllRoles;

public record GetAllRolesQuery() : IRequest<IReadOnlyCollection<RoleSummaryModel>>;
