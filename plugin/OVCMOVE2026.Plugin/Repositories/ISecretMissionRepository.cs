using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OVCMOVE2026.Plugin.Repositories;

public interface ISecretMissionRepository
{
    Task<SecretMission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SecretMission?> GetDetailAsync(Guid id, Guid teamId, CancellationToken cancellationToken = default);

    Task AddEvidencesAsync(Guid missionId, Guid submittedBy, List<EvidenceFile> evidences, CancellationToken cancellationToken = default);
    Task<EvidenceFile?> GetEvidenceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteEvidenceAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateClaimAsync(SecretMission mission, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecretMission>> GetMissionsWithoutQrCodeAsync(CancellationToken cancellationToken = default);
    Task UpdateQrCodeUrlAsync(Guid id, string qrCodeUrl, CancellationToken cancellationToken = default);

    Task<bool> HasAssignedMissionForTeamAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default, Guid? excludeMissionId = null);
    Task CreateAssignedMissionAsync(SecretMission mission, CancellationToken cancellationToken = default);
    Task UpdateMissionAsync(Guid missionId, Guid teamId, string name, string description, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Guid missionId, CancellationToken cancellationToken = default);
    Task<SecretMissionAdminDetailDto?> GetAdminDetailAsync(Guid id, CancellationToken cancellationToken = default);
}