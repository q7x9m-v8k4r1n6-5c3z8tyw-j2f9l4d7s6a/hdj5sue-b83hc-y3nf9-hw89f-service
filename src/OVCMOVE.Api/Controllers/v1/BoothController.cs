using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.DTOs.Booth;
using OVCMOVE.Application.Features.Booths.Commands.AcceptEntryToBooth;
using OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Booths.Query.GetMyBooth;
using System.Security.Claims;

namespace OVCMOVE.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public class BoothController : ControllerBase
{
    private readonly ISender _mediator;

    public BoothController(ISender mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// API Chấm điểm Trạm dành riêng cho Ban tổ chức (Organizer)
    /// Khi Đội quét QR xong, Organizer bấm các nút để cộng điểm 
    /// </summary>
    [HttpPost("submit-score")]
    [RequirePermission(PermissionCodes.BoothScoreSubmit)]
    public async Task<IActionResult> SubmitScore(
        [FromBody] BoothScoringRequestDTO request,
        CancellationToken cancellationToken)
    {
        //Tự động lấy ID của Organizer từ Token đăng nhập
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? string.Empty;

        var command = new SubmitBoothScoreCommand
        {
            BoothID = request.BoothID,
            TeamID = request.TeamID,
            OrganizerId = Guid.Parse(organizerId),
            Score = request.Score
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
            return BadRequest(ApiResponse.Error(ApiStatus.Codes.BadRequest, "Chấm điểm thất bại."));

        return Ok(ApiResponse.Success(new { Message = "Chấm điểm và ghi nhật ký thành công!" }));
    }

    /// <summary>
    /// API: Check-in Đội đua vào Trạm (Entry)
    /// </summary>
    [HttpPost("entry")]
    [RequirePermission(PermissionCodes.BoothEntryRequest)] // Đội đua quét QR vào trạm
    public async Task<IActionResult> Entry(
        [FromBody] EntryToBoothDto request,
        CancellationToken cancellationToken)
    {
        var command = new RequestEntryToBoothCommand
        {
            BoothId = request.BoothId,
            TeamId = request.TeamId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Error(ApiStatus.Codes.BadRequest, "Vào trạm thất bại."));

        return Ok(ApiResponse.Success(new { Message = "Đã gửi yêu cầu vào trạm! Vui lòng chờ Ban tổ chức duyệt." }));
    }

    /// <summary>
    /// API: Ban tổ chức (Organizer) duyệt cho Đội thi vào trạm
    /// </summary>
    [HttpPost("accept-entry")]
    [RequirePermission(PermissionCodes.BoothEntryManage)]
    public async Task<IActionResult> AcceptEntry(
        [FromBody] AcceptEntryToBoothDto request,
        CancellationToken cancellationToken)
    {
        var command = new AcceptEntryToBoothCommand
        {
            BoothId = request.BoothId,
            TeamId = request.TeamId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Error(ApiStatus.Codes.BadRequest, result.Message));

        return Ok(ApiResponse.Success(new { Message = result.Message }));
    }

    [HttpGet("my-booth")]
    [RequirePermission(PermissionCodes.BoothRead)]
    public async Task<IActionResult> GetMyBooth(
        [FromQuery] Guid raceId,
        CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? string.Empty;

        var boothId = await _mediator.Send(
            new GetMyBoothQuery { RaceId = raceId, OrganizerId = Guid.Parse(organizerId) },
            cancellationToken);

        if (boothId is null)
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                "Bạn chưa được gán vào trạm nào trong trận đấu này."));

        return Ok(ApiResponse.Success(new { BoothId = boothId }));
    }
}
