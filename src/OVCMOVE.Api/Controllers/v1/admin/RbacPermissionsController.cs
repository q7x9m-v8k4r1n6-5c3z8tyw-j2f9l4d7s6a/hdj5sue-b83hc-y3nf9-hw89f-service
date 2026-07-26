using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Controllers.v1;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.CreatePermission;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.DeletePermission;
using OVCMOVE.Application.Features.Rbac.Permissions.Command.UpdatePermission;
using OVCMOVE.Application.Features.Rbac.Permissions.Query.GetAllPermissions;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/rbac/permissions")]
public class RbacPermissionsController : BaseController<RbacPermissionsController>
{
    public RbacPermissionsController(ILogger<RbacPermissionsController> logger, IMediator mediator, IMapper mapper)
        : base(logger, mediator, mapper)
    {
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new GetAllPermissionsQuery(), cancellationToken);
            return Ok(new ApiResponseModel<IReadOnlyCollection<PermissionSummaryModel>>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing GetAll RBAC permissions.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> Create([FromBody] RbacContract.UpsertPermissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<CreatePermissionCommand>(request);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponseModel<PermissionSummaryModel>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Create RBAC permission.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpPut("{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> Update(Guid permissionId, [FromBody] RbacContract.UpsertPermissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = _mapper.Map<UpdatePermissionCommand>(request);
            command.PermissionId = permissionId;
            var result = await _mediator.Send(command, cancellationToken);
            if (result is null)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<PermissionSummaryModel>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Update RBAC permission.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }

    [HttpDelete("{permissionId:guid}")]
    [RequirePermission(PermissionCodes.RbacPermissionManage)]
    public async Task<IActionResult> Delete(Guid permissionId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(new DeletePermissionCommand { PermissionId = permissionId }, cancellationToken);
            if (!result)
            {
                return Ok(new ApiResponseModel<object>(APIContansts.StatusCode.NotFound, APIContansts.StatusMessage.NotFound));
            }

            return Ok(new ApiResponseModel<bool>(APIContansts.StatusCode.Success, APIContansts.StatusMessage.Success, data: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing Delete RBAC permission.");
            return Ok(new InternalServerErrorModel(ex.Message));
        }
    }
}
