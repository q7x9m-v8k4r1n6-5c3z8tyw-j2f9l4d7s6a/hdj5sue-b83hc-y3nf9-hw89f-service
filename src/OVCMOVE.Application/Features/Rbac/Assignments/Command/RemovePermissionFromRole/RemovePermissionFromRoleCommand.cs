using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommand : AuditedRequest, IRequest<bool>
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
