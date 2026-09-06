using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using MongoDB.Bson;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Application.Common;
using OVCMOVE2026.Plugin.Common;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE2026.Plugin.Controllers;

[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "plugin-2026")]
[Route("api/v1/plugin/cards")]
public sealed class CardController(IRaceCardService cardService) : ControllerBase
{
    [HttpGet("races/{raceId:guid}")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> GetAdminCards(Guid raceId, CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.GetAdminOverviewAsync(raceId, cancellationToken)));

    [HttpGet("races/{raceId:guid}/cards/{cardId}/teams")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> GetCardTeams(Guid raceId, string cardId, CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.GetCardTeamsAsync(raceId, cardId, cancellationToken)));

    [HttpPost("races/{raceId:guid}/inventory/restock")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> Restock(
        Guid raceId,
        [FromBody] RestockRequest request,
        CancellationToken cancellationToken)
    {
        await cardService.RestockAsync(raceId, request.Quantities, cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã nhập kho card."));
    }

    [HttpPut("races/{raceId:guid}/cards/{cardId}/config")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> UpdateConfig(
        Guid raceId,
        string cardId,
        [FromBody] CardConfigRequest request,
        CancellationToken cancellationToken)
    {
        await cardService.UpdateConfigAsync(raceId, cardId, request.Config, cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã lưu cấu hình card."));
    }

    [HttpPost("races/{raceId:guid}/cards/{cardId}/teams")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> Assign(
        Guid raceId,
        string cardId,
        [FromBody] AssignCardRequest request,
        CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.AssignAsync(
            raceId, cardId, request.TeamId, request.TeamName, request.Reason ?? string.Empty, cancellationToken)));

    [HttpDelete("races/{raceId:guid}/teams/{teamId:guid}/cards/{cardInstanceId:guid}")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> DeleteAssignment(
        Guid raceId,
        Guid teamId,
        Guid cardInstanceId,
        [FromBody] DeleteCardRequest request,
        CancellationToken cancellationToken)
    {
        await cardService.DeleteAssignmentAsync(raceId, cardInstanceId, teamId, request.Reason, cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã ghi nhận xóa card."));
    }

    [HttpPost("races/{raceId:guid}/revive-effects/{effectId}/confirm")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> ConfirmRevive(
        Guid raceId,
        string effectId,
        CancellationToken cancellationToken)
    {
        await cardService.ConfirmReviveAsync(
            raceId,
            effectId,
            GetRequiredCurrentUserId(),
            User.IsInRole("admin"),
            cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã xác nhận Revive."));
    }

    [HttpGet("team/races/{raceId:guid}/cards")]
    public async Task<IActionResult> GetTeamCards(Guid raceId, CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.GetTeamCardsAsync(
            raceId, GetRequiredCurrentUserId(), cancellationToken)));

    [HttpGet("team/races/{raceId:guid}/cards/{cardInstanceId:guid}")]
    public async Task<IActionResult> GetTeamCard(
        Guid raceId,
        Guid cardInstanceId,
        CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.GetTeamCardAsync(
            raceId, GetRequiredCurrentUserId(), cardInstanceId, cancellationToken)));

    [HttpPost("team/races/{raceId:guid}/cards/{cardInstanceId:guid}/use")]
    public async Task<IActionResult> UseCard(
        Guid raceId,
        Guid cardInstanceId,
        [FromBody] UseCardRequest request,
        CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.UseAsync(
            raceId,
            GetRequiredCurrentUserId(),
            cardInstanceId,
            request.CardUseId,
            ToBsonDocument(request.Inputs),
            cancellationToken)));

    private static BsonDocument ToBsonDocument(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return new BsonDocument();
        if (element.ValueKind != JsonValueKind.Object)
            throw new ApplicationValidationException("Inputs của card phải là JSON object.");
        return BsonDocument.Parse(element.GetRawText());
    }

    private Guid GetRequiredCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Token không hợp lệ.");
    }
}

public class RestockRequest
{
    [Required]
    public Dictionary<string, int> Quantities { get; init; } = [];
}

public sealed class CardConfigRequest
{
    [Required]
    public Dictionary<string, JsonElement> Config { get; init; } = [];
}

public sealed class AssignCardRequest
{
    [Required]
    public Guid TeamId { get; init; }

    [Required, MaxLength(255)]
    public string TeamName { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Reason { get; init; }
}

public sealed class DeleteCardRequest
{
    [Required, MinLength(1), MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}

public sealed class UseCardRequest
{
    [Required]
    public Guid CardUseId { get; init; }

    public JsonElement Inputs { get; init; }
}
