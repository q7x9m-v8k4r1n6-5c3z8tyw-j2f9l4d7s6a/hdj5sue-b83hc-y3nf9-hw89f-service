using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Team;
using OVCMOVE.Domain.Entities;
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

    public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _dapperHelper.ExecuteAsync(
                TeamQueries.AddTeamQuery(),
                new
                {
                    team.Id,
                    team.UserId,
                    team.TotalScore,
                    team.CreatedAt
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when adding team for user {UserId}", team.UserId);
            throw;
        }
    }

    public async Task<List<TeamAccountDetails>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _dapperHelper.QueryAsync<TeamAccountDetails>(
                TeamQueries.GetAllTeamsQuery(),
                cancellationToken: cancellationToken);
            return result.ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when getting all teams");
            throw;
        }
    }

    public async Task<List<TeamAccountDetails>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parameters = new { Keyword = $"%{keyword}%" };
            var result = await _dapperHelper.QueryAsync<TeamAccountDetails>(
                TeamQueries.SearchTeamQuery(),
                parameters,
                cancellationToken: cancellationToken);
            return result.ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when searching team");
            throw;
        }
    }
}
