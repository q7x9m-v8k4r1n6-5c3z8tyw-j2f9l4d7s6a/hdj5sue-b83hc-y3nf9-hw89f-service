using MediatR;
using Microsoft.Extensions.Configuration;
using OVCMOVE.Application.Abstractions;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Commands.SubmitTechCacheResult;

public class SubmitTechCacheResultCommandHandler
    : IRequestHandler<SubmitTechCacheResultCommand, SubmitTechCacheResultResult>
{
    private readonly ISecretMissionRepository _repository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public SubmitTechCacheResultCommandHandler(
        ISecretMissionRepository repository,
        IBlobStorageService blobStorageService,
        IConfiguration configuration)
    {
        _repository = repository;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
    }

    public async Task<SubmitTechCacheResultResult> Handle(
        SubmitTechCacheResultCommand request,
        CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission == null)
        {
            return new SubmitTechCacheResultResult { IsNotFound = true, Message = "Không tìm thấy nhiệm vụ này." };
        }

        if (mission.IsAssigned)
        {
            return new SubmitTechCacheResultResult { IsForbidden = true, Message = "Đây không phải Tech Cache." };
        }
        if (mission.Evidences.Any())
        {
            return new SubmitTechCacheResultResult
            {
                IsForbidden = true,
                Message = "Tech Cache này đã được nộp kết quả, không thể nộp lại.",
            };
        }

        if (mission.TeamId != request.TeamId)
        {
            return new SubmitTechCacheResultResult { IsForbidden = true, Message = "Nhiệm vụ không thuộc về đội của bạn." };
        }

        if (string.IsNullOrEmpty(mission.AccessCode) ||
            !string.Equals(mission.AccessCode.Trim(), request.Code.Trim(), StringComparison.Ordinal))
        {
            return new SubmitTechCacheResultResult { IsInvalidCode = true, Message = "Mã Tech Cache không đúng." };
        }

        var containerName = _configuration["OVCMOVE_AzureBlobStorage:EviContainerName"] ?? "mission-evidence";
        var url = await _blobStorageService.UploadAsync(
            request.Video.Stream, request.Video.FileName, request.Video.ContentType, containerName, cancellationToken);

        var evidence = new EvidenceFile
        {
            Id = Guid.NewGuid(),
            MissionId = request.MissionId,
            Url = url,
            FileType = "video",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.TeamId.ToString(),
        };
        await _repository.AddEvidencesAsync(request.MissionId, request.TeamId, new List<EvidenceFile> { evidence }, cancellationToken);

        var currentScore = await _repository.GetTeamScoreAsync(
            mission.RaceId!.Value, mission.TeamId!.Value, cancellationToken) ?? 0;

        var delta = request.Result == TechCacheResultType.Success ? mission.SuccessPoints : -mission.FailPoints;
        var newScore = currentScore + delta;

        await _repository.UpdateTeamScoreAsync(
            mission.RaceId.Value, mission.TeamId.Value, newScore, "system-tech-cache", cancellationToken);

        await _repository.InsertTechCacheScoringLogAsync(
            mission.RaceId.Value, mission.TeamId.Value, mission.Name, delta, currentScore, newScore,
            request.Result == TechCacheResultType.Success ? "tech_cache_success" : "tech_cache_fail",
            cancellationToken);

        var isMapPiece = request.Result == TechCacheResultType.Success && mission.SuccessPoints == 0;
        var message = request.Result == TechCacheResultType.Success
            ? (isMapPiece
                ? $"Chúc mừng! Đội đã nhận được mảnh bản đồ từ nhiệm vụ \"{mission.Name}\"!"
                : $"Chúc mừng! Đội được cộng {mission.SuccessPoints} điểm từ nhiệm vụ \"{mission.Name}\"!")
            : $"Rất tiếc! Đội bị trừ {mission.FailPoints} điểm từ nhiệm vụ \"{mission.Name}\".";

        return new SubmitTechCacheResultResult
        {
            IsSuccess = true,
            ScoreDelta = delta,
            IsMapPieceReward = isMapPiece,
            Message = message,
        };
    }
}