using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Security;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class TeamController : BaseController
{
    public TeamController(IMediator mediator)
        : base(mediator)
    {
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> GetAllTeams(
        [FromQuery] CommonContract.PagedRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = new GetAllTeamsQuery
        {
            Page = request.Page,
            PageSize = request.PageSize
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse.Success(result.ToResponse(
            item => item.ToResponse())));
    }
    [HttpGet("search")]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> SearchTeams([FromQuery] string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _mediator.Send(
            new SearchTeamQuery(query),
            cancellationToken);
        return Ok(ApiResponse.Success(
            result.Select(item => item.ToResponse()).ToArray()));
    }
}
