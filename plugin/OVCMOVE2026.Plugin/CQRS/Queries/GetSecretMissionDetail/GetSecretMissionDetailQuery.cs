using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Models.DTOs;
using OVCMOVE2026.Plugin.Repositories; // Import Repository

namespace OVCMOVE2026.Plugin.CQRS.Queries.GetSecretMissionDetail;

public sealed record GetSecretMissionDetailQuery(Guid Id, Guid TeamId) : IRequest<SecretMissionDetailDto?>;

public class GetSecretMissionDetailQueryHandler : IRequestHandler<GetSecretMissionDetailQuery, SecretMissionDetailDto?>
{
    // Đổi IDbExecutor thành ISecretMissionRepository
    private readonly ISecretMissionRepository _repository;

    public GetSecretMissionDetailQueryHandler(ISecretMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<SecretMissionDetailDto?> Handle(GetSecretMissionDetailQuery request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetDetailAsync(request.Id, request.TeamId, cancellationToken);
        if (mission == null) return null;

        EvidenceFileDto MapDto(EvidenceFile f) => new() { Id = f.Id, Url = f.Url, CreatedAt = f.CreatedAt };

        return new SecretMissionDetailDto
        {
            Id = mission.Id,
            Name = mission.Name,
            Description = mission.Description,
            IsAssigned = mission.IsAssigned,
            EvidenceImageUrls = mission.Evidences.Where(x => x.FileType == "image").Select(MapDto).ToList(),
            EvidenceVideoUrls = mission.Evidences.Where(x => x.FileType == "video").Select(MapDto).ToList()
        };
    }
}