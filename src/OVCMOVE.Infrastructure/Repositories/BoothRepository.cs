using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Infrastructure.Repositories;

public class BoothRepository : BaseRepository<BoothRepository>, IBoothRepository
{
    public BoothRepository(ILogger<BoothRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<Guid?> CreateAsync(Booth booth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RaceQueries.CreateBoothQuery(), booth);
        return affectedRows >= 1 ? booth.Id : null;
    }

    public async Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booths = await _dapperHelper.QueryAsync<Booth>(RaceQueries.GetBoothsByRaceIdQuery(), new { RaceId = raceId });
        return booths.ToArray();
    }

    public async Task<bool> UpdateAsync(Booth booth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dapperHelper.ExecuteAsync(RaceQueries.UpdateBoothQuery(), booth);
        return affectedRows >= 1;
    }

    public Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.ExecuteAsync(RaceQueries.DeleteBoothByIdQuery(), new { BoothId = boothId });
    }

    public Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dapperHelper.ExecuteAsync(RaceQueries.DeleteBoothsByRaceIdQuery(), new { RaceId = raceId });
    }
}
