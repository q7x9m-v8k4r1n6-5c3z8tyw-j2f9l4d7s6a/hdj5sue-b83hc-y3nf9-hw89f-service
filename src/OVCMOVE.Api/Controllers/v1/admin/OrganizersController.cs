using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Features.Organizers.Command.ChangeOrganizerStatus;
using OVCMOVE.Application.Features.Rbac.Roles.Query.GetAllRoles;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1.Admin;

[Route("api/v1/admin/organizers")]
public class OrganizersController : BaseController
{
    public OrganizersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.OrganizerManageAccounts)]
    public async Task<IActionResult> Create(
        [FromBody] OrganizerContract.CreateOrganizerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            request.ToCommand(),
            cancellationToken);
        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("roles")]
    [RequirePermission(PermissionCodes.OrganizerManageAccounts)]
    public async Task<IActionResult> GetAssignableRoles(CancellationToken cancellationToken)
    {
        var roles = await _mediator.Send(new GetAllRolesQuery(), cancellationToken);
        return Ok(ApiResponse.Success(roles.Select(role => new
        {
            role.Id,
            role.Name,
            role.Code,
            role.Description,
        }).ToArray()));
    }

    [HttpPatch("{organizerId:guid}/deactivate")]
    [RequirePermission(PermissionCodes.OrganizerManageAccounts)]
    public Task<IActionResult> Deactivate(
        Guid organizerId,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            organizerId,
            UserConstants.Status.Inactive,
            cancellationToken);

    [HttpPatch("{organizerId:guid}/activate")]
    [RequirePermission(PermissionCodes.OrganizerManageAccounts)]
    public Task<IActionResult> Activate(
        Guid organizerId,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(
            organizerId,
            UserConstants.Status.Active,
            cancellationToken);

    /// <summary>Sends one organizer status change and maps a missing account to HTTP 404.</summary>
    private async Task<IActionResult> ChangeStatusAsync(
        Guid organizerId,
        string status,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangeOrganizerStatusCommand
            {
                OrganizerId = organizerId,
                Status = status
            },
            cancellationToken);

        return result is null
            ? NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound))
            : Ok(ApiResponse.Success(result.ToResponse()));
    }
}
