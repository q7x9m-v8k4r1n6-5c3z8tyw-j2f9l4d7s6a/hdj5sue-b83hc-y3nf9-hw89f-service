using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Common; // <-- Dòng khắc phục lỗi
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Models;

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

    // ... using ...
public async Task<bool> Handle(SubmitMissionEvidenceCommand request, CancellationToken cancellationToken)
{
    var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
    if (mission == null) throw new InvalidOperationException("Nhiệm vụ không hợp lệ.");

    var containerName = _configuration["OVCMOVE_AzureBlobStorage:EviContainerName"] ?? "mission-evidence";
    var newEvidences = new List<EvidenceFile>();
    var now = DateTime.UtcNow;

    async Task ProcessFiles(IEnumerable<FileUploadModel>? files, string fileType)
    {
        if (files == null) return;
        foreach (var file in files)
        {
            var url = await _blobStorageService.UploadAsync(file.Stream, file.FileName, file.ContentType, containerName, cancellationToken);
            newEvidences.Add(new EvidenceFile
            {
                Id = Guid.NewGuid(), MissionId = request.MissionId,
                Url = url, FileType = fileType,
                CreatedAt = now, CreatedBy = request.SubmittedBy.ToString()
            });
        }
    }

    await ProcessFiles(request.Images, "image");
    await ProcessFiles(request.Videos, "video");

    if (!newEvidences.Any()) throw new InvalidOperationException("Phải nộp ít nhất 1 file.");

    await _repository.AddEvidencesAsync(request.MissionId, request.SubmittedBy, newEvidences, cancellationToken);
    return true;
}
}