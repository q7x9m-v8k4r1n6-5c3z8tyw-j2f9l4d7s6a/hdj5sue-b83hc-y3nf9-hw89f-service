using MediatR;
using Microsoft.AspNetCore.Mvc;

using OVCMOVE.Application.Common; 
using OVCMOVE2026.Plugin.Common; 
using OVCMOVE2026.Plugin.Models.Contracts; 
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionDetail;
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionOverview;
using OVCMOVE2026.Plugin.CQRS.Commands.SubmitMissionEvidence;
using OVCMOVE2026.Plugin.CQRS.Commands.ClaimSecretMission;
using OVCMOVE2026.Plugin.CQRS.Commands.GenerateMissionQrCodes;
using OVCMOVE2026.Plugin.CQRS.Commands.DeleteMissionEvidence;

namespace OVCMOVE2026.Plugin.Controllers;

[Route("api/v1/plugin/secret-mission")]
public class SecretMissionController : PluginBaseController 
{
    public SecretMissionController(IMediator mediator) : base(mediator) 
    {
    }

    [HttpPost("{id:guid}/evidence")]
    public async Task<IActionResult> SubmitEvidence(
        [FromRoute] Guid id,
        [FromForm] SubmitMissionEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        var submittedBy = GetRequiredCurrentUserId(); 

        var imageModels = new List<FileUploadModel>();
        if (request.Images?.Any() == true)
        {
            foreach (var file in request.Images)
            {
                var error = await MediaFileValidator.ValidateImageAsync(file, cancellationToken);
                if (error != null)
                {
                    return BadRequest(PluginResponse.Error(400, $"Lỗi file ảnh '{file.FileName}': {error}"));
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
                    return BadRequest(PluginResponse.Error(400, $"Lỗi file video '{file.FileName}': {error}"));
                }
                
                videoModels.Add(new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType));
            }
        }

        if (!imageModels.Any() && !videoModels.Any())
        {
            return BadRequest(PluginResponse.Error(400, "Vui lòng chọn ít nhất 1 ảnh hoặc 1 video."));
        }

        var command = new SubmitMissionEvidenceCommand(id, submittedBy, imageModels, videoModels);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(PluginResponse.Success(result, "Nộp bằng chứng thành công."));
    }

    [HttpGet("races/{raceId:guid}/overview")]
    public async Task<IActionResult> GetOverview(
        [FromRoute] Guid raceId,
        CancellationToken cancellationToken)
    {
        var teamId = GetRequiredCurrentUserId();

        var query = new GetSecretMissionOverviewQuery(teamId, raceId);
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(PluginResponse.Success(result, "Thành công"));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(
        [FromRoute] Guid id, 
        CancellationToken cancellationToken)
    {
        var teamId = GetRequiredCurrentUserId();

        var query = new GetSecretMissionDetailQuery(id, teamId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(PluginResponse.Error(404, "Không tìm thấy nhiệm vụ bí mật này hoặc chưa được giao cho đội của bạn."));
        }

        return Ok(PluginResponse.Success(result, "Thành công"));
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> ClaimMission(
        [FromRoute] Guid id, 
        CancellationToken cancellationToken)
    {
        var teamId = GetRequiredCurrentUserId();

        var command = new ClaimSecretMissionCommand(id, teamId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsNotFound)
        {
            return NotFound(PluginResponse.Error(404, result.Message));
        }
        
        if (result.IsConflict)
        {
            return StatusCode(409, PluginResponse.Error(409, result.Message));
        }

        return Ok(PluginResponse.Success(true, result.Message));
    }

    [HttpPost("generate-qrcodes")]
    public async Task<IActionResult> GenerateQrCodesBatch(
        CancellationToken cancellationToken)
    {
        var command = new GenerateMissionQrCodesBatchCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.TotalGenerated == 0 && result.TotalFailed == 0)
        {
            return Ok(PluginResponse.Success(result, "Tất cả các nhiệm vụ đều đã có mã QR. Không cần tạo thêm."));
        }

        return Ok(PluginResponse.Success(result, $"Xử lý hoàn tất. Tạo mới thành công: {result.TotalGenerated} mã. Bị lỗi: {result.TotalFailed} mã."));
    }

    [HttpDelete("{missionId}/evidence/{fileId}")]
    public async Task<IActionResult> DeleteEvidence(
        Guid missionId, 
        Guid fileId, 
        CancellationToken cancellationToken)
    {
        var command = new DeleteMissionEvidenceCommand(missionId, fileId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(PluginResponse.Error(404, "Không tìm thấy nhiệm vụ bí mật."));
        }

        return Ok(PluginResponse.Success(true, "Xóa file minh chứng thành công."));
    }
}