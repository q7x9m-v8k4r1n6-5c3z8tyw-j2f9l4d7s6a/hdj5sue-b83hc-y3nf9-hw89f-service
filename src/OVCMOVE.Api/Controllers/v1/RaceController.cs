using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Extensions;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Application.Features.Races.Query.GetRaceDetail;
using OVCMOVE.Application.Features.Races.Query.GetRaceMessages;
using OVCMOVE.Application.Features.Races.Query.GetRaceRules;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Constants;
using static OVCMOVE.Api.Contracts.RaceContract;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class RaceController : BaseController
{
    private static readonly System.Text.Json.JsonSerializerOptions PayloadJsonSerializerOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    public RaceController(IMediator mediator)
        : base(mediator)
    {
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.RaceRead)]
    public async Task<IActionResult> GetAllRaces([FromQuery] GetAllRacesRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsCurrentUserTeam())
        {
            request.TeamId = GetCurrentUserId() ?? Guid.Empty;
        }

        var query = request.ToQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("{raceId}")]
    [RequirePermission(PermissionCodes.RaceRead)]
    public async Task<IActionResult> GetRaceDetail([FromRoute] Guid raceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _mediator.Send(
            new GetRaceDetailQuery
            {
                RaceId = raceId,
                TeamId = IsCurrentUserTeam()
                    ? GetCurrentUserId() ?? Guid.Empty
                    : null
            },
            cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
        }

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> CreateRace([FromForm] RaceMutationFormRequest form, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileError = form.CoverImage is null
            ? null
            : await ImageFileValidator.ValidateAsync(
                form.CoverImage,
                cancellationToken);
        if (fileError is not null)
        {
            return BadRequest(ApiResponse.Error(
                ApiStatus.Codes.BadRequest,
                ApiStatus.Messages.BadRequest,
                fileError));
        }

        var request = JsonPayloadDeserializer.Deserialize<CreateNewRaceRequest>(
            form.Payload);
        var command = request.ToCommand();
        using var coverStream = form.CoverImage?.OpenReadStream();
        if (form.CoverImage is not null && coverStream is not null)
        {
            command.CoverImage = new FileUploadModel(
                coverStream,
                form.CoverImage.FileName,
                form.CoverImage.ContentType);
        }

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Success(result));
    }

    [HttpPatch("{raceId:guid}")]
    [Consumes("multipart/form-data")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> PatchRace(Guid raceId, [FromForm] RaceMutationFormRequest form, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fileError = form.CoverImage is null
            ? null
            : await ImageFileValidator.ValidateAsync(
                form.CoverImage,
                cancellationToken);
        if (fileError is not null)
        {
            return BadRequest(ApiResponse.Error(
                ApiStatus.Codes.BadRequest,
                ApiStatus.Messages.BadRequest,
                fileError));
        }

        var request = JsonPayloadDeserializer.Deserialize<PatchRaceRequest>(
            form.Payload);
        var command = request.ToCommand(raceId);
        using var coverStream = form.CoverImage?.OpenReadStream();
        if (form.CoverImage is not null && coverStream is not null)
        {
            command.CoverImage = new FileUploadModel(
                coverStream,
                form.CoverImage.FileName,
                form.CoverImage.ContentType);
        }

        var result = await _mediator.Send(command, cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
        }

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("leaderboard")]
    [RequirePermission(PermissionCodes.RaceLeaderboardRead)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] TeamLeaderboardRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = request.ToQuery();
        var result = await _mediator.Send(query, cancellationToken);
        var response = result.Select(item => item.ToResponse()).ToList();

        return Ok(ApiResponse.Success(response));
    }

    [HttpGet("booth-list")]
    [RequirePermission(PermissionCodes.BoothRead)]
    public async Task<IActionResult> GetBoothList(
        [FromQuery] BoothListRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = request.ToQuery();
        var result = await _mediator.Send(query, cancellationToken);
        var response = result.Select(item => item.ToResponse()).ToList();

        return Ok(ApiResponse.Success(response));
    }

    [HttpPatch("{raceId:guid}/teams/{teamId:guid}/score")]
    [RequirePermission(PermissionCodes.RaceScoreManage)]
    public async Task<IActionResult> UpdateTeamScore(
        [FromRoute] Guid raceId,
        [FromRoute] Guid teamId,
        [FromBody] UpdateTeamScoreRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _mediator.Send(
            request.ToCommand(raceId, teamId),
            cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
        }

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("scoring-log")]
    [RequirePermission(PermissionCodes.RaceLeaderboardRead)]
    public async Task<IActionResult> GetScoringLog(
        [FromQuery] ScoringLogRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = request.ToQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("{raceId:guid}/messages")]
    [RequirePermission(PermissionCodes.RaceRead)]
    public async Task<IActionResult> GetRaceMessages(
        [FromRoute] Guid raceId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _mediator.Send(
            new GetRaceMessagesQuery
            {
                RaceId = raceId,
                Limit = limit <= 0 ? 50 : limit
            },
            cancellationToken);

        return Ok(ApiResponse.Success(result
            .Where(CanCurrentUserReadRaceMessage)
            .Select(item => item.ToResponse())
            .ToArray()));
    }

    [HttpPost("{raceId:guid}/messages")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> SendRaceMessage(
        [FromRoute] Guid raceId,
        [FromBody] SendRaceMessageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _mediator.Send(
            request.ToCommand(
                raceId,
                GetRequiredCurrentUserId()),
            cancellationToken);
        if (result is null)
        {
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                ApiStatus.Messages.NotFound));
        }

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("{raceId:guid}/rules")]
    [RequirePermission(PermissionCodes.RaceRead)]
    public async Task<IActionResult> GetRaceRules([FromRoute] Guid raceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetRaceRulesQuery
            {
                RaceId = raceId,
                TeamId = GetRequiredCurrentUserId()
            },
            cancellationToken);

        if (!result.IsTeamInRace)
            return NotFound(ApiResponse.Error(ApiStatus.Codes.NotFound, "Bạn chưa được gán vào trận đấu này."));

        return Ok(ApiResponse.Success(new { result.Rules }));
    }

    [HttpGet("{raceId:guid}/rules/admin")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetAdminRaceRules([FromRoute] Guid raceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rules = await _mediator.Send(
            new GetAdminRaceRulesQuery { RaceId = raceId },
            cancellationToken);

        return Ok(ApiResponse.Success(new { Rules = rules }));
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return Guid.TryParse(userId, out var currentUserId)
            ? currentUserId
            : null;
    }

    private bool IsCurrentUserTeam() =>
        string.Equals(
            User.FindFirstValue("user_type"),
            UserConstants.UserType.Team,
            StringComparison.OrdinalIgnoreCase);

    private bool IsCurrentUserOrganizer() =>
        string.Equals(
            User.FindFirstValue("user_type"),
            UserConstants.UserType.Organizer,
            StringComparison.OrdinalIgnoreCase);

    private bool HasPermission(string permissionCode) =>
        User.Claims.Any(claim =>
            claim.Type == PermissionAuthorizationHandler.PermissionClaimType &&
            string.Equals(
                claim.Value,
                permissionCode,
                StringComparison.OrdinalIgnoreCase));

    private bool CanCurrentUserReadRaceMessage(RaceMessageResultModel message)
    {
        if (HasPermission(PermissionCodes.RaceManage))
        {
            return true;
        }

        var recipientKeys = message.RecipientKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (recipientKeys.Contains(RaceMessageRecipientConstants.All))
        {
            return true;
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return false;
        }

        if (IsCurrentUserTeam())
        {
            return recipientKeys.Contains(RaceMessageRecipientConstants.AllTeams) ||
                recipientKeys.Contains($"{RaceMessageRecipientConstants.TeamKeyPrefix}{currentUserId.Value:D}");
        }

        if (IsCurrentUserOrganizer())
        {
            return recipientKeys.Contains(RaceMessageRecipientConstants.AllOrganizers) ||
                recipientKeys.Contains($"{RaceMessageRecipientConstants.OrganizerKeyPrefix}{currentUserId.Value:D}");
        }

        return false;
    }

    private static T DeserializePayload<T>(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload không được để trống.");
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(
                    payload,
                    PayloadJsonSerializerOptions)
                ?? throw new ArgumentException("Payload không hợp lệ.");
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new ArgumentException(
                "Payload JSON không hợp lệ.",
                exception);
        }
    }
}
