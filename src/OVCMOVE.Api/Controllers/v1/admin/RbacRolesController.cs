using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Features.Rbac.Roles.Command.DeleteRole;
using OVCMOVE.Application.Features.Rbac.Roles.Query.GetAllRoles;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/rbac/roles")]
public class RbacRolesController : BaseController
{
    public RbacRolesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllRolesQuery(),
            cancellationToken);
        return Ok(ApiResponse.Success(
            result.Select(item => item.ToResponse()).ToArray()));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> Create(
        [FromBody] RbacContract.UpsertRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            request.ToCreateCommand(),
            cancellationToken);
        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpPut("{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> Update(
        Guid roleId,
        [FromBody] RbacContract.UpsertRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            request.ToUpdateCommand(roleId),
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound))
            : Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpDelete("{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> Delete(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(
            new DeleteRoleCommand
            {
                RoleId = roleId
            },
            cancellationToken);
        return deleted
            ? Ok(ApiResponse.Success(true))
            : NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
    }
}
