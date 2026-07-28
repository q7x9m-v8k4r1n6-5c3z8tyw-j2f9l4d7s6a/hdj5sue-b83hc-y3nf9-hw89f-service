using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.CreateRole;

public class CreateRoleCommand : AuditedRequest, IRequest<RoleSummaryModel>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}
