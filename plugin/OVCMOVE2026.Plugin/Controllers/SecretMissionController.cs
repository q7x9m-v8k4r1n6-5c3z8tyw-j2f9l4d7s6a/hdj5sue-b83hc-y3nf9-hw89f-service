using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;
using OVCMOVE.Application.Common; 
using OVCMOVE2026.Plugin.Common; 
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionDetail;
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionOverview;
using OVCMOVE2026.Plugin.CQRS.Commands.SubmitMissionEvidence;
using OVCMOVE2026.Plugin.CQRS.Commands.ClaimSecretMission;
using OVCMOVE2026.Plugin.CQRS.Commands.GenerateMissionQrCodes;
using OVCMOVE2026.Plugin.Models.Contracts;

namespace OVCMOVE2026.Plugin.Controllers;

[Route("api/v1/plugin/secret-mission")]
[ApiController] // Bắt buộc phải có khi dùng ControllerBase
public class SecretMissionController : ControllerBase // Kế thừa class gốc của Microsoft
{
    private readonly IMediator _mediator;

    // Tự Inject Mediator thay vì nhờ BaseController
    public SecretMissionController(IMediator mediator) 
    {
        _mediator = mediator;
    }

    [HttpPost("{id:guid}/evidence")]
    public async Task<IActionResult> SubmitEvidence(
        [FromRoute] Guid id,
        [FromForm] SubmitMissionEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var submittedBy))
        {
            // Trả về Object ẩn danh (Anonymous Object) có cấu trúc giống ApiResponse
            return Unauthorized(new { code = 401, message = "Không lấy được thông tin người dùng. Vui lòng đăng nhập lại." });
        }

        var imageModels = new List<FileUploadModel>();
        if (request.Images?.Any() == true)
        {
            foreach (var file in request.Images)
            {
                var error = await MediaFileValidator.ValidateImageAsync(file, cancellationToken);
                if (error != null)
                {
                    return BadRequest(new { code = 400, message = $"Lỗi file ảnh '{file.FileName}': {error}" });
                }
                
                imageModels.Add(new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType));
            }
        }

        var videoModels = new List<FileUploadModel>();
        if (request.Videos?.Any() == true)
        {
            foreach (var file in request.Videos)
            {
                var error = await MediaFileValidator.ValidateVideoAsync(file, cancellationToken);
                if (error != null)
                {
                    return BadRequest(new { code = 400, message = $"Lỗi file video '{file.FileName}': {error}" });
                }
                
                videoModels.Add(new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType));
            }
        }

        if (!imageModels.Any() && !videoModels.Any())
        {
            return BadRequest(new { code = 400, message = "Vui lòng chọn ít nhất 1 ảnh hoặc 1 video." });
        }

        var command = new SubmitMissionEvidenceCommand(id, submittedBy, imageModels, videoModels);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(new { code = 200, message = "Nộp bằng chứng thành công.", data = result });
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var teamIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teamIdString, out var teamId))
        {
            return Unauthorized(new { code = 401, message = "Không lấy được thông tin đội chơi." });
        }

        var query = new GetSecretMissionOverviewQuery(teamId);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new { code = 200, message = "Thành công", data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var teamIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teamIdString, out var teamId))
        {
            return Unauthorized(new { code = 401, message = "Không lấy được thông tin đội chơi." });
        }

        var query = new GetSecretMissionDetailQuery(id, teamId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { code = 404, message = "Không tìm thấy nhiệm vụ bí mật này hoặc chưa được giao cho đội của bạn." });
        }

        return Ok(new { code = 200, message = "Thành công", data = result });
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> ClaimMission([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var teamIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(teamIdString, out var teamId))
        {
            return Unauthorized(new { code = 401, message = "Không lấy được thông tin đội chơi." });
        }

        var command = new ClaimSecretMissionCommand(id, teamId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsNotFound)
        {
            return NotFound(new { code = 404, message = result.Message });
        }
        
        if (result.IsConflict)
        {
            return StatusCode(409, new { code = 409, message = result.Message });
        }

        return Ok(new { code = 200, message = result.Message, data = true });
    }

    /// <summary>
    /// Quét DB và tự động tạo mã QR cho các nhiệm vụ bí mật chưa có QrCodeUrl
    /// </summary>
    [HttpPost("generate-qrcodes")]
    public async Task<IActionResult> GenerateQrCodesBatch(CancellationToken cancellationToken)
    {
        var command = new GenerateMissionQrCodesBatchCommand();
        var result = await _mediator.Send(command, cancellationToken);

        // Nếu DB không trả về nhiệm vụ nào thiếu QR
        if (result.TotalGenerated == 0 && result.TotalFailed == 0)
        {
            return Ok(new 
            { 
                code = 200, 
                message = "Tất cả các nhiệm vụ đều đã có mã QR. Không cần tạo thêm.", 
                data = result 
            });
        }

        return Ok(new 
        { 
            code = 200, 
            message = $"Xử lý hoàn tất. Tạo mới thành công: {result.TotalGenerated} mã. Bị lỗi: {result.TotalFailed} mã.", 
            data = result 
        });
    }
    
}