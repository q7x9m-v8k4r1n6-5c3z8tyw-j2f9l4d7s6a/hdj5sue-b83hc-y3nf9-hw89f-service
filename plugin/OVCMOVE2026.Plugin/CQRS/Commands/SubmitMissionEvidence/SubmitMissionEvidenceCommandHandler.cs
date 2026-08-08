using MediatR;
using Microsoft.Extensions.Configuration;

using OVCMOVE.Application.Abstractions;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Commands.SubmitMissionEvidence;

public class SubmitMissionEvidenceCommandHandler : IRequestHandler<SubmitMissionEvidenceCommand, bool>
{
    private readonly ISecretMissionRepository _repository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public SubmitMissionEvidenceCommandHandler(
        ISecretMissionRepository repository,
        IBlobStorageService blobStorageService,
        IConfiguration configuration)
    {
        _repository = repository;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
    }

    public async Task<bool> Handle(SubmitMissionEvidenceCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission == null)
            throw new InvalidOperationException("Không tìm thấy nhiệm vụ bí mật này."); 
        if (!mission.IsAssigned)
            throw new InvalidOperationException("Không thể nộp bằng chứng vì nhiệm vụ này chưa được nhận.");

        var containerName = _configuration["OVCMOVE_AzureBlobStorage:EviContainerName"] ?? "mission-evidence";

        var imageUrls = new List<string>();
        if (request.Images?.Any() == true)
        {
            foreach (var image in request.Images)
            {
                var url = await _blobStorageService.UploadAsync(
                    image.Stream, image.FileName, image.ContentType, containerName, cancellationToken);
                imageUrls.Add(url);
            }
        }

        var videoUrls = new List<string>();
        if (request.Videos?.Any() == true)
        {
            foreach (var video in request.Videos)
            {
                var url = await _blobStorageService.UploadAsync(
                    video.Stream, video.FileName, video.ContentType, containerName, cancellationToken);
                videoUrls.Add(url);
            }
        }

        if (!imageUrls.Any() && !videoUrls.Any())
        {
            throw new InvalidOperationException("Phải nộp ít nhất 1 ảnh hoặc 1 video để hoàn thành nhiệm vụ.");
        }

        mission.EvidenceImageUrl = imageUrls.Any() ? imageUrls : null;
        mission.EvidenceVideoUrl = videoUrls.Any() ? videoUrls : null;
        
        mission.SubmittedBy = request.SubmittedBy;
        mission.SubmittedTime = DateTime.UtcNow;

        await _repository.UpdateEvidenceAsync(mission, cancellationToken);

        return true;
    }
}