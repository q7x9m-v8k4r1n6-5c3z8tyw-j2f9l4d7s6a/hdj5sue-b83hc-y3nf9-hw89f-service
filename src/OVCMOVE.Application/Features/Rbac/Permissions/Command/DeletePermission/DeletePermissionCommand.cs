using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac.Permissions.Command.DeletePermission;

public class DeletePermissionCommand : AuditedRequest, IRequest<bool>
{
    public Guid PermissionId { get; set; }
}
