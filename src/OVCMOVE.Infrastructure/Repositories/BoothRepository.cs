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
        string eventCode = "BOOTH",
        string eventName = "Chấm điểm trạm",
        string reasonCode = "BOOTH_COMPLETED",
        string reason = "Hoàn thành thử thách tại trạm",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Lấy thông tin Booth để truy vết RaceId
        var booth = await GetByIdAsync(boothId, cancellationToken);
        if (booth is null) return false;

        var currentScore = await _db.QueryFirstOrDefaultAsync<int>(
            "SELECT ISNULL(TotalScore, 0) FROM dbo.RaceTeam WHERE RaceID = @RaceId AND TeamID = @TeamId;",
            new { RaceId = booth.RaceId, TeamId = teamId },
            cancellationToken: cancellationToken);

        int scoreBefore = currentScore;
        int scoreAfter = scoreBefore + score;

        await _db.ExecuteAsync(
            BoothQueries.UpdateTeamScoreQuery(),
            new { BoothId = boothId, TeamId = teamId, Score = score },
            cancellationToken: cancellationToken);

        await _db.ExecuteAsync(
            BoothQueries.ReleaseBoothStatusQuery(),
            new { BoothId = boothId },
            cancellationToken: cancellationToken);

        await _db.ExecuteAsync(
            BoothQueries.InsertScoringLogQuery(),
            new
            {
                Id = Guid.NewGuid(),
                EventCode = eventCode,
                EventName = eventName,
                RaceId = booth.RaceId,
                TeamId = teamId,
                ActorId = organizerId,
                BoothId = boothId,
                Delta = score,
                ScoreBefore = scoreBefore,
                ScoreAfter = scoreAfter,
                ReasonCode = reasonCode,
                Reason = reason,
                CreatedBy = organizerId.ToString(),
                ModifiedBy = organizerId.ToString()
            },
            cancellationToken: cancellationToken);

        return true;
    }
}