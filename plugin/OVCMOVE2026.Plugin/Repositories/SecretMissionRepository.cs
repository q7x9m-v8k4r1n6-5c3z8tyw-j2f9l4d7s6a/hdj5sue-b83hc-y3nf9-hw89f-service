using System.Text.Json;
using OVCMOVE.Infrastructure.Persistence.Dapper; // Mượn IDbExecutor từ Core
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

    public async Task<SecretMission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Lưu ý từ Tech Lead: Tùy vào cấu hình Dapper của Core, nếu lúc chạy nó báo lỗi 
        // không parse được JSON sang List<string>, ta sẽ cần gắn thêm 1 cái TypeHandler cho Dapper.
        // Tạm thời cứ query chuẩn như vầy trước.
        return await _db.QueryFirstOrDefaultAsync<SecretMission>(
            SecretMissionQueries.GetByIdQuery(),
            new { Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task UpdateEvidenceAsync(SecretMission mission, CancellationToken cancellationToken = default)
    {
        // Chuyển List thành JSON string để lưu vào SQL Server
        var evidenceImageUrlJson = mission.EvidenceImageUrl != null 
            ? JsonSerializer.Serialize(mission.EvidenceImageUrl) 
            : null;
            
        var evidenceVideoUrlJson = mission.EvidenceVideoUrl != null 
            ? JsonSerializer.Serialize(mission.EvidenceVideoUrl) 
            : null;

        await _db.ExecuteAsync(
            SecretMissionQueries.UpdateEvidenceQuery(),
            new
            {
                mission.Id,
                EvidenceImageUrl = evidenceImageUrlJson,
                EvidenceVideoUrl = evidenceVideoUrlJson,
                mission.SubmittedBy,
                mission.SubmittedTime
            },
            cancellationToken: cancellationToken);
    }

    public async Task UpdateClaimAsync(SecretMission mission, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync(
            SecretMissionQueries.UpdateClaimQuery(),
            new
            {
                mission.Id,
                mission.TeamId,
                mission.ReceivedBy,
                mission.ReceivedTime
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<SecretMission>> GetMissionsWithoutQrCodeAsync(CancellationToken cancellationToken = default)
    {
        return await _db.QueryAsync<SecretMission>(
            SecretMissionQueries.GetMissionsWithoutQrCodeQuery(),
            cancellationToken: cancellationToken);
    }

    public async Task UpdateQrCodeUrlAsync(Guid id, string qrCodeUrl, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync(
            SecretMissionQueries.UpdateQrCodeUrlQuery(),
            new
            {
                Id = id,
                QrCodeUrl = qrCodeUrl
            },
            cancellationToken: cancellationToken);
    }
}