using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Security;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;
using OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;
using OVCMOVE.Application.Features.Organizers.Command.DeleteOrganizer;
using OVCMOVE.Application.Features.Organizers.Query.GetOrganizerDetail;
using OVCMOVE.Application.Features.Organizers.Command.UpdateOrganizer;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class OrganizerController : BaseController
{
    public OrganizerController(IMediator mediator)
        : base(mediator)
    {
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.OrganizerRead)]
    public async Task<IActionResult> GetAllOrganizers(
        [FromQuery] CommonContract.PagedRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = new GetAllOrganizersQuery
        {
            Search = request.Search,
            Page = request.Page,
            PageSize = request.PageSize
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse.Success(result.ToResponse(
            item => item.ToResponse())));
    }

    [HttpGet("search")]
    [RequirePermission(PermissionCodes.OrganizerRead)]
    public async Task<IActionResult> SearchOrganizers(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _mediator.Send(
            new SearchOrganizerQuery(query),
            cancellationToken);
        return Ok(ApiResponse.Success(
            result.Select(item => item.ToResponse()).ToArray()));
    }

    [HttpGet("{organizerId:guid}")]
    [RequirePermission(PermissionCodes.OrganizerRead)]
    public async Task<IActionResult> GetOrganizerDetail(Guid organizerId, CancellationToken cancellationToken)
    {
        var organizer = await _mediator.Send(new GetOrganizerDetailQuery(organizerId), cancellationToken);
        return organizer is null
            ? NotFound(ApiResponse.Error(ApiStatus.Codes.NotFound, ApiStatus.Messages.NotFound))
            : Ok(ApiResponse.Success(organizer));
    }

    [HttpPut("{organizerId:guid}")]
    [RequirePermission(PermissionCodes.OrganizerManageAccounts)]
    public async Task<IActionResult> UpdateOrganizer(Guid organizerId, [FromBody] OrganizerContract.UpdateOrganizerRequest request, CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateOrganizerCommand { OrganizerId = organizerId, DisplayName = request.DisplayName, RoleIds = request.RoleIds, Status = request.Status }, cancellationToken);
        return updated ? Ok(ApiResponse.Success(new { Id = organizerId })) : NotFound(ApiResponse.Error(ApiStatus.Codes.NotFound, ApiStatus.Messages.NotFound));
    }

    [HttpDelete("{organizerId:guid}")]
    [RequirePermission(PermissionCodes.OrganizerManageAccounts)]
    public async Task<IActionResult> DeleteOrganizer(
        Guid organizerId,
        CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(
            new DeleteOrganizerCommand { OrganizerId = organizerId }, cancellationToken);
        return deleted
            ? Ok(ApiResponse.Success(new { Id = organizerId }))
            : NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
    }
}
