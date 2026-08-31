using MediatR;
using Microsoft.AspNetCore.Mvc;
using OVCMOVE.Application.Common;
using OVCMOVE2026.Plugin.Common;
using OVCMOVE2026.Plugin.CQRS.Commands.ClaimSecretMission;
using OVCMOVE2026.Plugin.CQRS.Commands.CreateSecretMission;
using OVCMOVE2026.Plugin.CQRS.Commands.DeleteMissionEvidence;
using OVCMOVE2026.Plugin.CQRS.Commands.DeleteSecretMission;
using OVCMOVE2026.Plugin.CQRS.Commands.GenerateMissionQrCodes;
using OVCMOVE2026.Plugin.CQRS.Commands.SubmitMissionEvidence;
using OVCMOVE2026.Plugin.CQRS.Commands.UpdateSecretMission;
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionAdminDetail;
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionAdminOverview;
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionDetail;
using OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionOverview;
using OVCMOVE2026.Plugin.Models.Contracts;

namespace OVCMOVE2026.Plugin.Controllers;

[Route("api/v1/plugin/secret-mission")]
public class SecretMissionController(IMediator mediator) : PluginBaseController(mediator)
{
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
                    return BadRequest(PluginResponse.Error(400, $"Lỗi file ảnh '{file.FileName}': {error}"));

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
                    return BadRequest(PluginResponse.Error(400, $"Lỗi file video '{file.FileName}': {error}"));

                videoModels.Add(new FileUploadModel(file.OpenReadStream(), file.FileName, file.ContentType));
            }
        }

        if (!imageModels.Any() && !videoModels.Any())
            return BadRequest(PluginResponse.Error(400, "Vui lòng chọn ít nhất 1 ảnh hoặc 1 video."));

        var result = await _mediator.Send(
            new SubmitMissionEvidenceCommand(id, submittedBy, imageModels, videoModels),
            cancellationToken);
        return Ok(PluginResponse.Success(result, "Nộp bằng chứng thành công."));
    }

    [HttpGet("races/{raceId:guid}/overview")]
    public async Task<IActionResult> GetOverview(Guid raceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSecretMissionOverviewQuery(GetRequiredCurrentUserId(), raceId),
            cancellationToken);
        return Ok(PluginResponse.Success(result, "Thành công"));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSecretMissionDetailQuery(id, GetRequiredCurrentUserId()),
            cancellationToken);
        return result is null
            ? NotFound(PluginResponse.Error(404, "Không tìm thấy nhiệm vụ bí mật này hoặc chưa được giao cho đội của bạn."))
            : Ok(PluginResponse.Success(result, "Thành công"));
    }

    [HttpPost("{id:guid}/claim")]
    public async Task<IActionResult> ClaimMission(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ClaimSecretMissionCommand(id, GetRequiredCurrentUserId()),
            cancellationToken);
        if (result.IsNotFound)
            return NotFound(PluginResponse.Error(404, result.Message));
        if (result.IsConflict)
            return StatusCode(409, PluginResponse.Error(409, result.Message));
        return Ok(PluginResponse.Success(true, result.Message));
    }

    [HttpPost("generate-qrcodes")]
    public async Task<IActionResult> GenerateQrCodesBatch(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateMissionQrCodesBatchCommand(),
            cancellationToken);
        var message = result.TotalGenerated == 0 && result.TotalFailed == 0
            ? "Tất cả các nhiệm vụ đều đã có mã QR. Không cần tạo thêm."
            : $"Xử lý hoàn tất. Tạo mới thành công: {result.TotalGenerated} mã. Bị lỗi: {result.TotalFailed} mã.";
        return Ok(PluginResponse.Success(result, message));
    }

    [HttpDelete("{missionId}/evidence/{fileId}")]
    public async Task<IActionResult> DeleteEvidence(
        Guid missionId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteMissionEvidenceCommand(missionId, fileId),
            cancellationToken);
        return result
            ? Ok(PluginResponse.Success(true, "Xóa file minh chứng thành công."))
            : NotFound(PluginResponse.Error(404, "Không tìm thấy nhiệm vụ bí mật."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateMission(
        [FromBody] CreateSecretMissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateSecretMissionCommand(request.RaceId, request.TeamId, request.Name, request.Description),
            cancellationToken);
        if (!result.IsSuccess)
            return result.IsConflict
                ? StatusCode(409, PluginResponse.Error(409, result.Message))
                : BadRequest(PluginResponse.Error(400, result.Message));
        return Ok(PluginResponse.Success(new { missionId = result.MissionId }, result.Message));
    }

    [HttpGet("races/{raceId:guid}/admin-overview")]
    public async Task<IActionResult> GetAdminOverview(Guid raceId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSecretMissionAdminOverviewQuery(raceId),
            cancellationToken);
        return Ok(PluginResponse.Success(result, "Thành công"));
    }

    [HttpGet("{id:guid}/admin")]
    public async Task<IActionResult> GetAdminDetail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSecretMissionAdminDetailQuery(id),
            cancellationToken);
        return result is null
            ? NotFound(PluginResponse.Error(404, "Không tìm thấy nhiệm vụ bí mật này."))
            : Ok(PluginResponse.Success(result, "Thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMission(
        Guid id,
        [FromBody] UpdateSecretMissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateSecretMissionCommand(id, request.TeamId, request.Name, request.Description),
            cancellationToken);
        if (result.IsNotFound)
            return NotFound(PluginResponse.Error(404, result.Message));
        if (result.IsConflict)
            return StatusCode(409, PluginResponse.Error(409, result.Message));
        if (!result.IsSuccess)
            return BadRequest(PluginResponse.Error(400, result.Message));
        return Ok(PluginResponse.Success(true, result.Message));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMission(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteSecretMissionCommand(id),
            cancellationToken);
        return result
            ? Ok(PluginResponse.Success(true, "Xóa nhiệm vụ thành công."))
            : NotFound(PluginResponse.Error(404, "Không tìm thấy nhiệm vụ bí mật."));
    }
}
