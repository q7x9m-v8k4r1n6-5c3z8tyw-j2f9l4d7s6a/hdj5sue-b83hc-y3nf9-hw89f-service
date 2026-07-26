using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommand : BaseRequestModel, IRequest<bool>
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
