using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.RemoveRoleFromUser;

public class RemoveRoleFromUserCommand : BaseRequestModel, IRequest<bool>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
