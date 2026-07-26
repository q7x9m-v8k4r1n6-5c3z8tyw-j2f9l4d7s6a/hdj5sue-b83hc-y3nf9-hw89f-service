using MediatR;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignRoleToUser;

public class AssignRoleToUserCommand : BaseRequestModel, IRequest<UserRoleAssignmentModel?>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
