using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using OVCMOVE.Application.DTOs.Booth;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

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
    [Authorize(Roles = "Organizer")]
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
            OrganizerId = organizerId,
            Score = request.Score
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
            return BadRequest(new { Message = "Chấm điểm thất bại." });

        return Ok(new { Message = "Chấm điểm và ghi nhật ký thành công!" });
    }
}