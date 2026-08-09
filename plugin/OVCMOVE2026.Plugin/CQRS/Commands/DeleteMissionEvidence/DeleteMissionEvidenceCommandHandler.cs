using MediatR;
using Microsoft.Extensions.Configuration;
using OVCMOVE.Application.Abstractions;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.CQRS.Commands.DeleteMissionEvidence;

public class DeleteMissionEvidenceCommandHandler : IRequestHandler<DeleteMissionEvidenceCommand, bool>
{
    private readonly ISecretMissionRepository _repository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public DeleteMissionEvidenceCommandHandler(
        ISecretMissionRepository repository,
        IBlobStorageService blobStorageService,
        IConfiguration configuration)
    {
        _repository = repository;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
    }

    public async Task<bool> Handle(DeleteMissionEvidenceCommand request, CancellationToken cancellationToken)
    {
        var file = await _repository.GetEvidenceByIdAsync(request.FileId, cancellationToken);
        if (file == null || file.MissionId != request.MissionId) return true; // Idempotent

        await _repository.DeleteEvidenceAsync(request.FileId, cancellationToken);

        var containerName = _configuration["OVCMOVE_AzureBlobStorage:EviContainerName"] ?? "mission-evidence";
        await _blobStorageService.TryDeleteAsync(file.Url, containerName, cancellationToken);

        return true;
    }
}