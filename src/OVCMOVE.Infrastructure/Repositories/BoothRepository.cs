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
}
