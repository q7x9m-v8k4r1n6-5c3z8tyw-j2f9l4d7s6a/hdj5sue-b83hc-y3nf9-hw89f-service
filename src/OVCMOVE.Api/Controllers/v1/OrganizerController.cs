using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Security;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;
using OVCMOVE.Application.Features.Organizers.Query.SearchOrganizer;

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
}
