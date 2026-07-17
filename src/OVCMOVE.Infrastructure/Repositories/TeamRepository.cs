using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Infrastructure.Repositories;

public class TeamRepository : BaseRepository<TeamRepository>, ITeamRepository
{
    public TeamRepository(ILogger<TeamRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<List<Team>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sqlQuery = TeamQueries.GetAllTeamsQuery();
            var result = await _dapperHelper.QueryAsync<Team>(sqlQuery);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error when getting all teams");
            throw;
        }
    }

    public async Task<List<Team>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sqlQuery = TeamQueries.SearchTeamQuery();
            var parameters = new { Keyword = $"%{keyword}%" };
            var result = await _dapperHelper.QueryAsync<Team>(sqlQuery, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error when searching team");
            throw;
        }
    }
}