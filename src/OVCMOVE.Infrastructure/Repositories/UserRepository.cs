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

    public Task<User?> GetByUsernameAnyStatusAsync(
        string username,
        CancellationToken cancellationToken = default) =>
        _db.QueryFirstOrDefaultAsync<User>(
            UserQueries.GetByUsernameAnyStatusQuery(),
            new { Username = username },
            cancellationToken: cancellationToken);

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

    public async Task UpdateGoogleProfileAsync(
        Guid id,
        string? displayName,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync(UserQueries.UpdateGoogleProfileQuery(), new
        {
            Id = id,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl,
        }, cancellationToken: cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        Guid id,
        string userType,
        string modifiedBy,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _db.ExecuteAsync(
            UserQueries.SoftDeleteQuery(),
            new { id, userType, modifiedBy, modifiedAt },
            cancellationToken: cancellationToken);
        return affectedRows == 1;
    }
}
