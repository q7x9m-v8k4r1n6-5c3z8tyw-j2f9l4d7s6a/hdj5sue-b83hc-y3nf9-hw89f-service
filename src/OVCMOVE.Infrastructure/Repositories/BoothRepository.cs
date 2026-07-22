using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class BoothRepository : BaseRepository<BoothRepository>, IBoothRepository
{
    public BoothRepository(ILogger<BoothRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<Guid?> CreateAsync(Booth booth, CancellationToken cancellationToken = default)
    {
        {
            cancellationToken.ThrowIfCancellationRequested();

            var affectedRows = await _dapperHelper.ExecuteAsync(RaceQueries.CreateBoothQuery(), booth);
            return affectedRows >= 1 ? booth.Id : null;

        }
    }
    public Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.ExecuteAsync(RaceQueries.DeleteBoothsByRaceIdQuery(), new { RaceId = raceId });
    }
    public async Task<Booth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dapperHelper.QueryFirstOrDefaultAsync<Booth>(
            BoothQueries.GetBoothByIdQuery(),
            new { Id = id }
        );
    }

    public async Task<bool> SubmitScoreAndReleaseAsync(
    Guid boothId,
    Guid teamId,
    string organizerId,
    int score,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        //Cộng điểm tích lũy cho Team
        var affectedScoreRows = await _dapperHelper.ExecuteAsync(
            BoothQueries.UpdateTeamScoreQuery(),
            new { Score = score, TeamId = teamId }
        );

        //Trả lại trạng thái trống cho Trạm
        var affectedBoothRows = await _dapperHelper.ExecuteAsync(
            BoothQueries.ReleaseBoothStatusQuery(),
            new { BoothId = boothId }
        );

        //Nhật ký chấm điểm
        var affectedLogRows = await _dapperHelper.ExecuteAsync(
            BoothQueries.InsertScoringLogQuery(),
            new
            {
                Id = Guid.NewGuid(),
                BoothId = boothId,
                TeamId = teamId,
                OrganizerId = organizerId,
                ScoreGiven = score
            }
        );

        return affectedScoreRows > 0 && affectedBoothRows > 0 && affectedLogRows > 0;
    }
}
