using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class TeamRepository : BaseRepository<TeamRepository>, ITeamRepository
{
    public TeamRepository(ILogger<TeamRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<List<GetAllTeamsResultModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string sqlQuery = TeamQueries.GetAllTeamsQuery();
        var result = await _dapperHelper.QueryAsync<GetAllTeamsResultModel>(sqlQuery);
        return result.ToList();
    }

    public async Task<List<SearchTeamResultModel>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string sqlQuery = TeamQueries.SearchTeamQuery();
        var parameters = new { Keyword = $"%{keyword}%" };

        var result = await _dapperHelper.QueryAsync<SearchTeamResultModel>(sqlQuery, parameters);
        return result.ToList();
    }
}