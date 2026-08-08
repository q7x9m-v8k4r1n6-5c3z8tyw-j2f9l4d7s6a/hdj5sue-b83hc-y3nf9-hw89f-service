using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Repositories;

public interface ISecretMissionRepository
{
    Task<SecretMission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateEvidenceAsync(SecretMission mission, CancellationToken cancellationToken = default);
    Task UpdateClaimAsync(SecretMission mission, CancellationToken cancellationToken = default);
}