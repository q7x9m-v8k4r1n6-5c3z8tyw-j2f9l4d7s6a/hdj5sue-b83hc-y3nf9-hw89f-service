using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Security;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;
using OVCMOVE.Application.Features.Teams.Command.CreateTeam;
using OVCMOVE.Application.Features.Teams.Command.UpdateTeam;
using OVCMOVE.Application.Features.Teams.Query.GetTeamDetail;
using OVCMOVE.Application.Features.Teams.Command.DeleteTeam;
using OVCMOVE.Application.Features.Teams.Command.ResetTeamPassword;
using OVCMOVE.Application.Features.Teams.Query.ScoreHistory;
using OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class TeamController : BaseController
{
    public TeamController(IMediator mediator)
        : base(mediator)
    {
    }

    [HttpGet("leaderboard")]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] TeamContract.LeaderboardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new TeamLeaderboardQuery(
                request.RaceId!.Value,
                GetCurrentTeamId()),
            cancellationToken);

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("score-history")]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> GetScoreHistory(
        [FromQuery] TeamContract.ScoreHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ScoreHistoryQuery(
                request.RaceId!.Value,
                GetCurrentTeamId(),
                request.Page,
                request.PageSize),
            cancellationToken);

        return Ok(ApiResponse.Success(result.ToResponse(
            item => item.ToResponse())));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> CreateTeam(
        [FromBody] TeamContract.CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateTeamCommand
        {
            DisplayName = request.DisplayName,
            Email = request.Email,
        }, cancellationToken);

        return Ok(ApiResponse.Success(new TeamContract.CreateTeamResponse
        {
            Id = result.Id,
            Username = result.Username,
        }));
    }

    [HttpGet("{teamId:guid}")]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> GetTeamDetail(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await _mediator.Send(
            new GetTeamDetailQuery(teamId),
            cancellationToken);
        if (team is null)
        {
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
        }

        return Ok(ApiResponse.Success(new TeamContract.TeamDetailResponse
        {
            Id = team.Id,
            Name = team.DisplayName ?? team.Username ?? team.LinkedEmail,
            Username = team.Username ?? string.Empty,
            LeaderEmail = team.LinkedEmail,
            Status = team.Status,
        }));
    }

    [HttpPut("{teamId:guid}")]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> UpdateTeam(
        Guid teamId,
        [FromBody] TeamContract.UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _mediator.Send(new UpdateTeamCommand
        {
            TeamId = teamId,
            DisplayName = request.DisplayName,
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            ResetPassword = request.ResetPassword,
            Status = request.Status,
        }, cancellationToken);
        if (!updated)
        {
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
        }

        return Ok(ApiResponse.Success(new { Id = teamId }));
    }

    [HttpDelete("{teamId:guid}")]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> DeleteTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(
            new DeleteTeamCommand { TeamId = teamId }, cancellationToken);
        return deleted
            ? Ok(ApiResponse.Success(new { Id = teamId }))
            : NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
    }

    [HttpPost("{teamId:guid}/reset-password")]
    [RequirePermission(PermissionCodes.TeamRead)]
    public async Task<IActionResult> ResetPassword(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var reset = await _mediator.Send(
            new ResetTeamPasswordCommand { TeamId = teamId }, cancellationToken);
        return reset
            ? Ok(ApiResponse.Success(new { Id = teamId }))
            : NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
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
            Search = request.Search,
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

    private Guid GetCurrentTeamId()
    {
        var teamIdValue =
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(teamIdValue, out var teamId)
            ? teamId
            : throw new UnauthorizedAccessException("Token không hợp lệ.");
    }
}
