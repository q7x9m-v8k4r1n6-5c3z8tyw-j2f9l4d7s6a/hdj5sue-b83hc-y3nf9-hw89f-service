using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;
using OVCMOVE.Application.Common;
using Microsoft.Data.SqlClient;

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

    public async Task<bool> TryRequestEntryAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var affectedRows = await _db.ExecuteAsync(
                BoothQueries.TryRequestBoothEntryQuery(),
                new { BoothId = boothId, TeamId = teamId },
                cancellationToken);
            return affectedRows == 1;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public async Task<bool> TryOccupyAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int affectedRows;
        try
        {
            affectedRows = await _db.ExecuteAsync(
                BoothQueries.TryOccupyBoothQuery(),
                new { BoothId = boothId, TeamId = teamId },
                cancellationToken);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return false;
        }

        return affectedRows == 1;
    }

    public async Task<bool> TryReleaseAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            BoothQueries.TryReleaseBoothQuery(),
            new { BoothId = boothId, TeamId = teamId },
            cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryRejectEntryAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.ExecuteAsync(
            BoothQueries.TryRejectBoothEntryQuery(),
            new { BoothId = boothId, TeamId = teamId },
            cancellationToken);

        return affectedRows == 1;
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
    SubmitBoothScoreModel model,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booth = await GetByIdAsync(model.BoothId, cancellationToken);
        if (booth is null) return false;

        var currentScore = await _db.QueryFirstOrDefaultAsync<int?>(
            BoothQueries.GetRaceTeamScoreQuery(),
            new { RaceId = booth.RaceId, TeamId = model.TeamId },
            cancellationToken: cancellationToken);

        if (currentScore is null)
        {
            throw new ApplicationValidationException(
                "Đội không tham gia giải đua của trạm này.");
        }

        var scoreBefore = currentScore.Value;
        var scoreAfter = scoreBefore + model.Score;

        var scoreRows = await _db.ExecuteAsync(
            BoothQueries.UpdateTeamScoreQuery(),
            new { BoothId = model.BoothId, TeamId = model.TeamId, Score = model.Score },
            cancellationToken: cancellationToken);

        if (scoreRows != 1)
        {
            throw new ApplicationConflictException(
                "Đội không còn chiếm trạm này.");
        }

        var releaseRows = await _db.ExecuteAsync(
            BoothQueries.ReleaseBoothStatusQuery(),
            new { BoothId = model.BoothId, TeamId = model.TeamId },
            cancellationToken: cancellationToken);

        if (releaseRows != 1)
        {
            throw new ApplicationConflictException(
                "Không thể giải phóng trạm sau khi chấm điểm.");
        }

        try
        {
            var logRows = await _db.ExecuteAsync(
                BoothQueries.InsertScoringLogQuery(),
                new
                {
                    Id = Guid.NewGuid(),
                    EventCode = model.EventCode,
                    EventName = model.EventName,
                    RaceId = booth.RaceId,
                    TeamId = model.TeamId,
                    ActorId = model.OrganizerId,
                    BoothId = model.BoothId,
                    Delta = model.Score,
                    ScoreBefore = scoreBefore,
                    ScoreAfter = scoreAfter,
                    ReasonCode = model.ReasonCode,
                    Reason = model.Reason,
                    CreatedBy = model.OrganizerId.ToString(),
                    ModifiedBy = model.OrganizerId.ToString()
                },
                cancellationToken: cancellationToken);
            PersistenceWriteGuard.EnsureInserted(logRows, nameof(ScoringLog));
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            throw new ApplicationConflictException(
                "Đội đã hoàn thành trạm này.",
                exception);
        }

        return true;
    }
}
