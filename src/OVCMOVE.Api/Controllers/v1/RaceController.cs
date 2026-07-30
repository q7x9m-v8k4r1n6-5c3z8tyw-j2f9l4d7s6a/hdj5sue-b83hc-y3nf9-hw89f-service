using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.CreateRace;
using OVCMOVE.Application.Features.Races.Command.PatchRace;
using OVCMOVE.Application.Features.Races.Query.GetAllRaces;
using OVCMOVE.Application.Features.Races.Query.GetRaceDetail;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using static OVCMOVE.Api.Contracts.RaceContract;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class RaceController : BaseController
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public RaceController(IMediator mediator)
        : base(mediator)
    {
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllRaces([FromQuery] GetAllRacesRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = request.ToQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(ApiResponse.Success(result.ToResponse()));
    }

    [HttpGet("{raceId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRaceDetail([FromRoute] Guid raceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _mediator.Send(
            new GetRaceDetailQuery { RaceId = raceId },
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

        var request = DeserializePayload<CreateNewRaceRequest>(form.Payload);
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

        var request = DeserializePayload<PatchRaceRequest>(form.Payload);
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

    private static T DeserializePayload<T>(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload không được để trống.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, JsonOptions)
                ?? throw new ArgumentException("Payload không hợp lệ.");
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Payload JSON không hợp lệ.",
                exception);
        }
    }

    [HttpGet("leaderboard")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] TeamLeaderboardRequest request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = request.ToQuery();
        var result = await _mediator.Send(
            query, 
            cancellationToken);
        var response = result.Select(
            item => item.ToResponse()).ToList();

        return Ok(ApiResponse.Success(response));
    }

    [HttpGet("booth-list")]
    [RequirePermission(PermissionCodes.RaceManage)]
    public async Task<IActionResult> GetBoothList(
        [FromQuery] BoothListRequest request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = request.ToQuery();
        var result = await _mediator.Send(
            query, 
            cancellationToken);
        var response = result.Select(
            item => item.ToResponse()).ToList();

        return Ok(ApiResponse.Success(response));
    }
}
