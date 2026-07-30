using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly IDbExecutor _db;

    public TeamRepository(IDbExecutor db) =>
        _db = db;

    public async Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IEnumerable<Guid> teamIds,
        CancellationToken cancellationToken = default)
    {
        var ids = teamIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var existingIds = await _db.QueryAsync<Guid>(
            TeamQueries.GetExistingIdsQuery(),
            new
            {
                Ids = ids,
                UserType = UserConstants.UserType.Team
            },
            cancellationToken: cancellationToken);
        return existingIds.ToArray();
    }

    public async Task<(IReadOnlyCollection<User> Items, int TotalItems)> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new
        {
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            UserType = UserConstants.UserType.Team,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };
        var result = await _db.QueryAsync<User>(
            TeamQueries.GetAllTeamsQuery(),
            parameters,
            cancellationToken: cancellationToken);
        var totalItems = await _db.QueryFirstOrDefaultAsync<int>(
            TeamQueries.CountTeamsQuery(),
            new { UserType = UserConstants.UserType.Team, Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim() },
            cancellationToken: cancellationToken);
        return (result.ToArray(), totalItems);
    }

    public async Task<IReadOnlyCollection<User>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new
        {
            Keyword = $"%{keyword}%",
            UserType = UserConstants.UserType.Team
        };
        var result = await _db.QueryAsync<User>(
            TeamQueries.SearchTeamQuery(),
            parameters,
            cancellationToken: cancellationToken);
        return result.ToArray();
    }

    public Task<User?> GetByIdAsync(
        Guid teamId,
        CancellationToken cancellationToken = default) =>
        _db.QueryFirstOrDefaultAsync<User>(
            TeamQueries.GetTeamByIdQuery(),
            new { TeamId = teamId, UserType = UserConstants.UserType.Team },
            cancellationToken: cancellationToken);

    public async Task<bool> UpdateAsync(
        User team,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _db.ExecuteAsync(
            TeamQueries.UpdateTeamQuery(),
            new
            {
                team.Id,
                team.Username,
                LinkedEmail = team.LinkedEmail,
                team.PasswordHash,
                team.DisplayName,
                team.Status,
                team.ModifiedBy,
                team.ModifiedAt,
                UserType = UserConstants.UserType.Team,
            },
            cancellationToken: cancellationToken);
        return affectedRows == 1;
    }
}
