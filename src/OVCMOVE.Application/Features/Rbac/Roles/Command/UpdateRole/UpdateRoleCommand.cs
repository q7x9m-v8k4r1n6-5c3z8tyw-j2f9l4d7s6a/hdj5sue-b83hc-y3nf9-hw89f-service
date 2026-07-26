using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.UpdateRole;

public class UpdateRoleCommand : BaseRequestModel, IRequest<RoleSummaryModel?>
{
    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}
