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

    public async Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _dapperHelper.QueryFirstOrDefaultAsync<Team>(
                TeamQueries.GetByIdQuery(),
                new { TeamId = teamId },
                cancellationToken: cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when getting team by id {TeamId}", teamId);
            throw;
        }
    }

    public async Task<Team?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _dapperHelper.QueryFirstOrDefaultAsync<Team>(
                TeamQueries.GetByUsernameQuery(),
                new { Username = username },
                cancellationToken: cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when getting team by username {Username}", username);
            throw;
        }
    }

    public async Task<Team?> GetByLeaderEmailAsync(string leaderEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _dapperHelper.QueryFirstOrDefaultAsync<Team>(
                TeamQueries.GetByLeaderEmailQuery(),
                new { LeaderEmail = leaderEmail },
                cancellationToken: cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when getting team by leader email {LeaderEmail}", leaderEmail);
            throw;
        }
    }

    public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _dapperHelper.ExecuteAsync(
                TeamQueries.AddTeamQuery(),
                team,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when adding team {Username}", team.Username);
            throw;
        }
    }

    public async Task<List<Team>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sqlQuery = TeamQueries.GetAllTeamsQuery();
            var result = await _dapperHelper.QueryAsync<Team>(sqlQuery, cancellationToken: cancellationToken);
            return result.ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when getting all teams");
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
            var result = await _dapperHelper.QueryAsync<Team>(sqlQuery, parameters, cancellationToken: cancellationToken);
            return result.ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error when searching team");
            throw;
        }
    }
}
