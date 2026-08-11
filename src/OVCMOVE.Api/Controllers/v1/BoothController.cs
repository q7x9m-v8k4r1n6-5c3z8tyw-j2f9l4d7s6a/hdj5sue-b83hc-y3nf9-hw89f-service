using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Api.Mapping;
using OVCMOVE.Api.Security;
using OVCMOVE.Application.Features.Booths.Commands.AcceptEntryToBooth;
using OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Booths.Query.GetMyBooth;
using OVCMOVE.Application.Features.Booths.Commands.CancelBoothSession;
using OVCMOVE.Application.Features.Booths.Commands.RejectEntryToBooth;

namespace OVCMOVE.Api.Controllers.v1;

[Route("api/v1/[controller]")]
public class BoothController : BaseController
{
    public BoothController(IMediator mediator)
        : base(mediator)
    {
    }

    /// <summary>
    /// API Chấm điểm Trạm dành riêng cho Ban tổ chức (Organizer)
    /// Khi Đội quét QR xong, Organizer bấm các nút để cộng điểm 
    /// </summary>
    [HttpPost("submit-score")]
    [RequirePermission(PermissionCodes.BoothScoreSubmit)]
    public async Task<IActionResult> SubmitScore(
        [FromBody] BoothContract.SubmitScoreRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SubmitBoothScoreCommand
        {
            BoothID = request.BoothId,
            TeamID = request.TeamId,
            OrganizerId = GetRequiredCurrentUserId(),
            Score = request.Score
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
            return BadRequest(ApiResponse.Error(ApiStatus.Codes.BadRequest, "Chấm điểm thất bại."));

        return Ok(ApiResponse.Success(
            new BoothContract.OperationResponse(
                "Chấm điểm và ghi nhật ký thành công!")));
    }

    /// <summary>
    /// API: Check-in Đội đua vào Trạm (Entry)
    /// </summary>
    [HttpPost("entry")]
    [RequirePermission(PermissionCodes.BoothEntryRequest)]
    public async Task<IActionResult> Entry(
        [FromBody] BoothContract.EntryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RequestEntryToBoothCommand
        {
            BoothId = request.BoothId,
            TeamId = GetRequiredCurrentUserId()
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Error(
                ApiStatus.Codes.BadRequest,
                ApiStatus.Messages.BadRequest,
                result.Message));

        return Ok(ApiResponse.Success(
            new BoothContract.OperationResponse(result.Message)));
    }

    /// <summary>
    /// API: Ban tổ chức (Organizer) duyệt cho Đội thi vào trạm
    /// </summary>
    [HttpPost("accept-entry")]
    [RequirePermission(PermissionCodes.BoothEntryManage)]
    public async Task<IActionResult> AcceptEntry(
        [FromBody] BoothContract.AcceptEntryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AcceptEntryToBoothCommand
        {
            BoothId = request.BoothId,
            TeamId = request.TeamId,
            OrganizerId = GetRequiredCurrentUserId()
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Error(ApiStatus.Codes.BadRequest, result.Message));

        return Ok(ApiResponse.Success(
            new BoothContract.OperationResponse(result.Message)));
    }

    [HttpPost("reject-entry")]
    [RequirePermission(PermissionCodes.BoothEntryManage)]
    public async Task<IActionResult> RejectEntry(
        [FromBody] BoothContract.RejectEntryRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new RejectEntryToBoothCommand(
                request.BoothId,
                request.TeamId,
                GetRequiredCurrentUserId()),
            cancellationToken);

        return Ok(ApiResponse.Success(
            new BoothContract.OperationResponse(
                "Đã từ chối yêu cầu vào trạm.")));
    }

    [HttpPost("{boothId:guid}/cancel-session")]
    [RequirePermission(PermissionCodes.BoothEntryManage)]
    public async Task<IActionResult> CancelSession(
        Guid boothId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CancelBoothSessionCommand(
                boothId,
                GetRequiredCurrentUserId()),
            cancellationToken);

        return Ok(ApiResponse.Success(
            new BoothContract.OperationResponse(
                "Đã hủy lượt chơi và giải phóng trạm.")));
    }

    [HttpGet("my-booth")]
    [RequirePermission(PermissionCodes.BoothRead)]
    public async Task<IActionResult> GetMyBooth(
        [FromQuery] Guid raceId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMyBoothQuery
            {
                RaceId = raceId,
                OrganizerId = GetRequiredCurrentUserId()
            },
            cancellationToken);

        if (result is null)
            return NotFound(ApiResponse.Error(
                ApiStatus.Codes.NotFound,
                "Bạn chưa được gán vào trạm nào trong trận đấu này."));

        return Ok(ApiResponse.Success(result.ToResponse()));
    }

}
