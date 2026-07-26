using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Controllers.v1;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignPermissionToRole;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.AssignRoleToUser;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.RemovePermissionFromRole;
using OVCMOVE.Application.Features.Rbac.Assignments.Command.RemoveRoleFromUser;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/rbac/assignments")]
public class RbacAssignmentsController : BaseController<RbacAssignmentsController>
{
    public RbacAssignmentsController(ILogger<RbacAssignmentsController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpPost("users/{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> AssignRoleToUser(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new AssignRoleToUserCommand { UserId = userId, RoleId = roleId }, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<UserRoleAssignmentModel>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while assigning role {RoleId} to user {UserId}.", roleId, userId);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpDelete("users/{userId:guid}/roles/{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> RemoveRoleFromUser(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new RemoveRoleFromUserCommand { UserId = userId, RoleId = roleId }, cancellationToken);
            if (!result)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<bool>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing role {RoleId} from user {UserId}.", roleId, userId);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPost("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> AssignPermissionToRole(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new AssignPermissionToRoleCommand { RoleId = roleId, PermissionId = permissionId }, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<RolePermissionAssignmentModel>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while assigning permission {PermissionId} to role {RoleId}.", permissionId, roleId);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacAssignmentManage)]
    public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, Guid permissionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new RemovePermissionFromRoleCommand { RoleId = roleId, PermissionId = permissionId }, cancellationToken);
            if (!result)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<bool>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing permission {PermissionId} from role {RoleId}.", permissionId, roleId);
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
