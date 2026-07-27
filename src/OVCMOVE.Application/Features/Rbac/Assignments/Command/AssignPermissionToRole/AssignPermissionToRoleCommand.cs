using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignPermissionToRole;

public class AssignPermissionToRoleCommand : AuditedRequest, IRequest<RolePermissionAssignmentModel?>
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
