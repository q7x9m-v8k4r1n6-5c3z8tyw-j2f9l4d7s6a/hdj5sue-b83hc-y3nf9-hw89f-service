using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

/// <summary>
/// Repository xử lý truy xuất dữ liệu cho Trạm (Booth) sử dụng Dapper IDbExecutor.
/// </summary>
public class BoothRepository : IBoothRepository
{
    private readonly IDbExecutor _db;

    public BoothRepository(IDbExecutor db) =>
        _db = db;

    public async Task<Guid> CreateAsync(Booth booth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.CreateBoothQuery(),
            booth,
            cancellationToken: cancellationToken);

        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(Booth));

        return booth.Id;
    }

    public async Task<Booth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _db.QueryFirstOrDefaultAsync<Booth>(
            BoothQueries.GetBoothByIdQuery(),
            new { Id = id },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booths = await _db.QueryAsync<Booth>(
            RaceQueries.GetBoothsByRaceIdQuery(),
            new { RaceId = raceId },
            cancellationToken: cancellationToken);

        return booths.ToArray();
    }

    public async Task<bool> UpdateAsync(Booth booth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            RaceQueries.UpdateBoothQuery(),
            booth,
            cancellationToken: cancellationToken);

        return affectedRows >= 1;
    }

    public Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _db.ExecuteAsync(
            RaceQueries.DeleteBoothByIdQuery(),
            new { BoothId = boothId },
            cancellationToken: cancellationToken);
    }

    public async Task<bool> SubmitScoreAndReleaseAsync(
        Guid boothId,
        Guid teamId,
        Guid organizerId,
        int score,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Cập nhật điểm cho Đội
        await _db.ExecuteAsync(
            BoothQueries.UpdateTeamScoreQuery(),
            new { BoothId= boothId, TeamId = teamId, Score = score },
            cancellationToken: cancellationToken);

        // 2. Giải phóng trạng thái Trạm
        await _db.ExecuteAsync(
            BoothQueries.ReleaseBoothStatusQuery(),
            new { BoothId = boothId },
            cancellationToken: cancellationToken);

        // 3. Ghi Log nhập điểm
        await _db.ExecuteAsync(
            BoothQueries.InsertScoringLogQuery(),
            new { Id = Guid.NewGuid(), BoothId = boothId, TeamId = teamId, OrganizerId = organizerId, ScoreGiven = score },
            cancellationToken: cancellationToken);

        return true;
    }
}