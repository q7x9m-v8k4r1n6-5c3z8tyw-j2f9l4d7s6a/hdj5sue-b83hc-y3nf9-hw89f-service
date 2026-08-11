using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OVCMOVE2026.Plugin.Models;

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
}