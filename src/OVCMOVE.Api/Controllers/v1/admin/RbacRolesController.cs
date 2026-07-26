using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Controllers.v1;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Application.Features.Rbac.Roles.Command.CreateRole;
using OVCMOVE.Application.Features.Rbac.Roles.Command.DeleteRole;
using OVCMOVE.Application.Features.Rbac.Roles.Command.UpdateRole;
using OVCMOVE.Application.Features.Rbac.Roles.Query.GetAllRoles;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/rbac/roles")]
public class RbacRolesController : BaseController<RbacRolesController>
{
    public RbacRolesController(ILogger<RbacRolesController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetAllRolesQuery(), cancellationToken);
            return Ok(new ApiResponseModel<IReadOnlyCollection<RoleSummaryModel>>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing GetAll RBAC roles.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> Create([FromBody] RbacContract.UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<CreateRoleCommand>(request);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponseModel<RoleSummaryModel>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Create RBAC role.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPut("{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> Update(Guid roleId, [FromBody] RbacContract.UpsertRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<UpdateRoleCommand>(request);
            command.RoleId = roleId;
            var result = await _mediator.Send(command, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<RoleSummaryModel>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Update RBAC role.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpDelete("{roleId:guid}")]
    [RequirePermission(PermissionCodes.RbacRoleManage)]
    public async Task<IActionResult> Delete(Guid roleId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new DeleteRoleCommand { RoleId = roleId }, cancellationToken);
            if (!result)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<bool>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Delete RBAC role.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
