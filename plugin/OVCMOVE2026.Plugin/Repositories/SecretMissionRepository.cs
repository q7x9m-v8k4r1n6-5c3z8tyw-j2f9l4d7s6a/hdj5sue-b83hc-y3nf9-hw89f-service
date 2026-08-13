using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories.Queries;

namespace OVCMOVE2026.Plugin.Repositories;

public class SecretMissionRepository : ISecretMissionRepository
{
    private readonly IDbExecutor _db;

    public SecretMissionRepository(IDbExecutor db)
    {
        _db = db;
    }

    private async Task<SecretMission?> QueryMissionWithEvidences(string missionSql, object param, CancellationToken cancellationToken)
    {
        var mission = await _db.QueryFirstOrDefaultAsync<SecretMission>(missionSql, param, cancellationToken);
        
        if (mission == null) return null;

        var evidences = await _db.QueryAsync<EvidenceFile>(
            SecretMissionQueries.GetEvidencesByMissionIdQuery(),
            new { MissionId = mission.Id },
            cancellationToken);

        mission.Evidences = evidences?.ToList() ?? new List<EvidenceFile>();
        
        return mission;
    }
    public Task<SecretMission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QueryMissionWithEvidences(
            SecretMissionQueries.GetByIdQuery(), 
            new { Id = id }, 
            cancellationToken);

    public Task<SecretMission?> GetDetailAsync(Guid id, Guid teamId, CancellationToken cancellationToken = default) =>
        QueryMissionWithEvidences(
            SecretMissionQueries.GetDetailByIdAndTeamIdQuery(), 
            new { Id = id, TeamId = teamId }, 
            cancellationToken);

    public async Task AddEvidencesAsync(Guid missionId, Guid submittedBy, List<EvidenceFile> evidences, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync(
            SecretMissionQueries.UpdateMissionSubmitStateQuery(), 
            new { Id = missionId, SubmittedBy = submittedBy }, 
            cancellationToken);
        
        if (evidences.Any())
        {
            await _db.ExecuteAsync(
                SecretMissionQueries.InsertEvidenceQuery(), 
                evidences, 
                cancellationToken);
        }
    }

    public async Task<EvidenceFile?> GetEvidenceByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.QueryFirstOrDefaultAsync<EvidenceFile>(
            SecretMissionQueries.GetEvidenceByIdQuery(), 
            new { Id = id }, 
            cancellationToken);

    public async Task DeleteEvidenceAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.ExecuteAsync(
            SecretMissionQueries.DeleteEvidenceQuery(), 
            new { Id = id }, 
            cancellationToken);

    public async Task UpdateClaimAsync(SecretMission mission, CancellationToken cancellationToken = default) =>
        await _db.ExecuteAsync(
            SecretMissionQueries.UpdateClaimQuery(), 
            mission, 
            cancellationToken);

    public async Task<IEnumerable<SecretMission>> GetMissionsWithoutQrCodeAsync(CancellationToken cancellationToken = default) =>
        await _db.QueryAsync<SecretMission>(
            SecretMissionQueries.GetMissionsWithoutQrCodeQuery(), 
            cancellationToken: cancellationToken);

    public async Task UpdateQrCodeUrlAsync(Guid id, string qrCodeUrl, CancellationToken cancellationToken = default) =>
        await _db.ExecuteAsync(
            SecretMissionQueries.UpdateQrCodeUrlQuery(), 
            new { Id = id, QrCodeUrl = qrCodeUrl }, 
            cancellationToken);
}