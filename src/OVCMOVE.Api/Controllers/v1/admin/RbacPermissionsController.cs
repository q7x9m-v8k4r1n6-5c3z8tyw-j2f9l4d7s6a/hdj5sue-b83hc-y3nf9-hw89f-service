using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.DeletePermission;
using OVCMOVE.Application.Features.Rbac.Permissions.Query.GetAllPermissions;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/rbac/permissions")]
public class RbacPermissionsController : BaseController
{
    public RbacPermissionsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllPermissionsQuery(),
            cancellationToken);
        return Ok(ApiResponse.Success(
            result.Select(item => item.ToResponse()).ToArray()));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> Create(
        [FromBody] RbacContract.UpsertPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            request.ToCreateCommand(),
            cancellationToken);
        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpPut("{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> Update(
        Guid permissionId,
        [FromBody] RbacContract.UpsertPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            request.ToUpdateCommand(permissionId),
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound))
            : Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpDelete("{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> Delete(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(
            new DeletePermissionCommand
            {
                PermissionId = permissionId
            },
            cancellationToken);
        return deleted
            ? Ok(ApiResponse.Success(true))
            : NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
    }
}
