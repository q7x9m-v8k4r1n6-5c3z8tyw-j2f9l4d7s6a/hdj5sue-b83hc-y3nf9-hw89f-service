using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("races/{raceId:guid}/store/open")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> OpenStore(Guid raceId, CancellationToken cancellationToken)
    {
        await cardService.SetStoreOpenAsync(raceId, true, cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã mở cửa hàng card."));
    }

    [HttpPost("races/{raceId:guid}/store/close")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> CloseStore(Guid raceId, CancellationToken cancellationToken)
    {
        await cardService.SetStoreOpenAsync(raceId, false, cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã đóng cửa hàng card."));
    }

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

    [HttpPost("races/{raceId:guid}/inventory/schedule")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> ScheduleRestock(
        Guid raceId,
        [FromBody] ScheduleRestockRequest request,
        CancellationToken cancellationToken)
    {
        await cardService.ScheduleRestockAsync(raceId, request.ScheduledAt, request.Quantities, cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã hẹn giờ nhập kho card."));
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

    [HttpDelete("races/{raceId:guid}/cards/{cardId}/teams/{teamId:guid}")]
    [Authorize(Roles = "admin,organizer")]
    public async Task<IActionResult> DeleteAssignment(
        Guid raceId,
        string cardId,
        Guid teamId,
        [FromBody] DeleteCardRequest request,
        CancellationToken cancellationToken)
    {
        await cardService.DeleteAssignmentAsync(raceId, cardId, teamId, request.Reason, cancellationToken);
        return Ok(PluginResponse.Success(true, "Đã ghi nhận xóa card."));
    }

    [HttpGet("team/races/{raceId:guid}/cards")]
    public async Task<IActionResult> GetTeamCards(Guid raceId, CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.GetTeamCardsAsync(
            raceId, GetRequiredCurrentUserId(), cancellationToken)));

    [HttpGet("team/races/{raceId:guid}/cards/{cardId}")]
    public async Task<IActionResult> GetTeamCard(
        Guid raceId,
        string cardId,
        CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.GetTeamCardAsync(
            raceId, GetRequiredCurrentUserId(), cardId, cancellationToken)));

    [HttpPost("team/races/{raceId:guid}/cards/{cardId}/use")]
    public async Task<IActionResult> UseCard(
        Guid raceId,
        string cardId,
        [FromBody] UseCardRequest request,
        CancellationToken cancellationToken) =>
        Ok(PluginResponse.Success(await cardService.UseAsync(
            raceId, GetRequiredCurrentUserId(), cardId, request.Inputs, cancellationToken)));

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

public sealed class ScheduleRestockRequest : RestockRequest
{
    [Required]
    public DateTime ScheduledAt { get; init; }
}

public sealed class CardConfigRequest
{
    [Required]
    public Dictionary<string, string> Config { get; init; } = [];
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
    public Dictionary<string, string> Inputs { get; init; } = [];
}
