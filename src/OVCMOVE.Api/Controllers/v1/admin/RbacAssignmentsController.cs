using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Security;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignPermissionToRole;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignRoleToUser;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.RemovePermissionFromRole;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.RemoveRoleFromUser;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/rbac/assignments")]
public class RbacAssignmentsController : BaseController
{
    public RbacAssignmentsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("users/{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> AssignRoleToUser(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignRoleToUserCommand
            {
                UserId = userId,
                RoleId = roleId
            },
            cancellationToken);
        return result is null
            ? NotFoundResponse()
            : Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpDelete("users/{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> RemoveRoleFromUser(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var removed = await _mediator.Send(
            new RemoveRoleFromUserCommand
            {
                UserId = userId,
                RoleId = roleId
            },
            cancellationToken);
        return ToActionResult(removed);
    }

    [HttpPost("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> AssignPermissionToRole(
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AssignPermissionToRoleCommand
            {
                RoleId = roleId,
                PermissionId = permissionId
            },
            cancellationToken);
        return result is null
            ? NotFoundResponse()
            : Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> RemovePermissionFromRole(
        Guid roleId,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var removed = await _mediator.Send(
            new RemovePermissionFromRoleCommand
            {
                RoleId = roleId,
                PermissionId = permissionId
            },
            cancellationToken);
        return ToActionResult(removed);
    }

    private IActionResult ToActionResult(bool removed) =>
        removed
            ? Ok(ApiResponse.Success(true))
            : NotFoundResponse();

    private IActionResult NotFoundResponse() =>
        NotFound(ApiResponse.Error(
            ApiStatus.Codes.NotFound,
            ApiStatus.Messages.NotFound));
}
