using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;
using OVCMOVE.Infrastructure.Common;

namespace OVCMOVE.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbExecutor _db;

    public UserRepository(IDbExecutor db) =>
        _db = db;

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var sql = UserQueries.GetByUsernameQuery();
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            sql,
            new { Username = username, Status = UserConstants.Status.Active },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var sql = UserQueries.GetByEmailQuery();
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            sql,
            new { LinkedEmail = email, Status = UserConstants.Status.Active },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task<User?> GetByEmailAnyStatusAsync(string email, CancellationToken cancellationToken = default)
    {
        var sql = UserQueries.GetByEmailAnyStatusQuery();
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            sql,
            new { LinkedEmail = email },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = UserQueries.GetByIdQuery();
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            sql,
            new { Id = id, Status = UserConstants.Status.Active },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task<User?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken = default)
    {
        var sql = UserQueries.GetByShortNameQuery();
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            sql,
            new { ShortName = shortName },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var affectedRows = await _db.ExecuteAsync(
            UserQueries.AddUserQuery(),
            user,
            cancellationToken: cancellationToken);
        PersistenceWriteGuard.EnsureInserted(affectedRows, nameof(User));
    }

    public async Task UpdateDisplayNameAsync(Guid id, string displayName, CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync(
            UserQueries.UpdateDisplayNameQuery(),
            new { Id = id, DisplayName = displayName },
            cancellationToken: cancellationToken);
    }
}
