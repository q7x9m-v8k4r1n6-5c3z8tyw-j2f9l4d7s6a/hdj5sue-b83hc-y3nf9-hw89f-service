using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac.Roles.Command.DeleteRole;

public class DeleteRoleCommand : BaseRequestModel, IRequest<bool>
{
    public Guid RoleId { get; set; }
}
