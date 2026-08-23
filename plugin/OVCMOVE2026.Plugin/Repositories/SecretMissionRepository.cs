using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Models.DTOs;
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
    public async Task<bool> HasAssignedMissionForTeamAsync(
      Guid raceId, Guid teamId, CancellationToken cancellationToken = default, Guid? excludeMissionId = null)
    {
        var count = await _db.QueryFirstOrDefaultAsync<int>(
            SecretMissionQueries.CheckTeamHasAssignedMissionQuery(),
            new { RaceId = raceId, TeamId = teamId, ExcludeMissionId = excludeMissionId },
            cancellationToken);
        return count > 0;
    }

    public async Task CreateAssignedMissionAsync(
        SecretMission mission, CancellationToken cancellationToken = default) =>
        await _db.ExecuteAsync(
            SecretMissionQueries.CreateAssignedMissionQuery(),
            mission,
            cancellationToken);
    public async Task UpdateMissionAsync(
    Guid missionId, Guid teamId, string name, string description, CancellationToken cancellationToken = default) =>
    await _db.ExecuteAsync(
        SecretMissionQueries.UpdateMissionQuery(),
        new { Id = missionId, TeamId = teamId, Name = name, Description = description },
        cancellationToken);
    public async Task SoftDeleteAsync(Guid missionId, CancellationToken cancellationToken = default) =>
        await _db.ExecuteAsync(
            SecretMissionQueries.SoftDeleteQuery(),
            new { Id = missionId },
            cancellationToken);

    public async Task<SecretMissionAdminDetailDto?> GetAdminDetailAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var mission = await _db.QueryFirstOrDefaultAsync<SecretMissionAdminDetailDto>(
            SecretMissionQueries.GetAdminDetailByIdQuery(),
            new { Id = id },
            cancellationToken);

        if (mission == null) return null;

        var evidences = await _db.QueryAsync<EvidenceFile>(
            SecretMissionQueries.GetEvidencesByMissionIdQuery(),
            new { MissionId = id },
            cancellationToken);

        mission.EvidenceImageUrls = evidences
            .Where(e => e.FileType == "image")
            .Select(e => new EvidenceFileDto { Id = e.Id, Url = e.Url, CreatedAt = e.CreatedAt })
            .ToList();
        mission.EvidenceVideoUrls = evidences
            .Where(e => e.FileType == "video")
            .Select(e => new EvidenceFileDto { Id = e.Id, Url = e.Url, CreatedAt = e.CreatedAt })
            .ToList();

        return mission;
    }
}