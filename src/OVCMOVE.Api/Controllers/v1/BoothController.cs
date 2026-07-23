using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using AutoMapper;
using OVCMOVE.Api.Common;
using OVCMOVE.Api.Contracts;
using OVCMOVE.Application.DTOs.Booth;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Booths.Query.GetBoothSummary;
using OVCMOVE.Application.Features.Booths.Query.GetBoothDetail;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public class BoothController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IMapper _mapper;

    public BoothController(ISender mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
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

    /// <summary>
    /// API lấy tổng quan tất cả các trạm (phục vụ danh sách & SignalR)
    /// </summary>
    [HttpGet("status-summary")]
    public async Task<IActionResult> GetBoothSummary([FromQuery] BoothContract.GetBoothSummaryRequest request, CancellationToken cancellationToken)
    {
        var query = _mapper.Map<GetBoothSummaryQuery>(request ?? new BoothContract.GetBoothSummaryRequest());
        var result = await _mediator.Send(query, cancellationToken);
        var response = _mapper.Map<List<BoothContract.GetBoothSummaryResponse>>(result);

        return Ok(new ApiResponseModel<List<BoothContract.GetBoothSummaryResponse>>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });
    }

    /// <summary>
    /// API lấy chi tiết 1 trạm (hiện tên Đội đang chiếm)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBoothDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new GetBoothDetailQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);
        var response = _mapper.Map<BoothContract.GetBoothDetailResponse>(result);
        
        return Ok(new ApiResponseModel<BoothContract.GetBoothDetailResponse>
        {
            StatusCode = APIContansts.StatusCode.Success,
            Message = APIContansts.StatusMessage.Success,
            Data = response
        });
    }
}