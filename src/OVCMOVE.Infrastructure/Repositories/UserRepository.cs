using Microsoft.Extensions.Logging;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities; 
using OVCMOVE.Domain.Constants; 
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class UserRepository : BaseRepository<UserRepository>, IUserRepository
{
    public UserRepository(ILogger<UserRepository> logger, IDapperHelper dapperHelper) 
        : base(logger, dapperHelper)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var sql = UserQueryHelper.GetByUsernameQuery();
        var user =  await _dapperHelper.QueryFirstOrDefaultAsync<User>(
            sql,
            new { Username = username, Status = UserConstant.Status.Active },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var sql = UserQueryHelper.GetByEmailQuery();
        var user = await _dapperHelper.QueryFirstOrDefaultAsync<User>(
            sql,
            new { Email = email, Status = UserConstant.Status.Active },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task<User?> GetByEmailAnyStatusAsync(string email, CancellationToken cancellationToken = default)
    {
        var sql = UserQueryHelper.GetByEmailAnyStatusQuery();
        var user = await _dapperHelper.QueryFirstOrDefaultAsync<User>(
            sql,
            new { Email = email },
            cancellationToken: cancellationToken);

        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = UserQueryHelper.GetByIdQuery();
        var user =  await _dapperHelper.QueryFirstOrDefaultAsync<User>(
            sql,
            new { Id = id, Status = UserConstant.Status.Active },
            cancellationToken: cancellationToken);

        return user;
    } 

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dapperHelper.ExecuteAsync(
            UserQueryHelper.AddUserQuery(),
            user,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateDisplayNameAsync(Guid id, string displayName, CancellationToken cancellationToken = default)
    {
        await _dapperHelper.ExecuteAsync(
            UserQueryHelper.UpdateDisplayNameQuery(),
            new { Id = id, DisplayName = displayName },
            cancellationToken: cancellationToken);
    }
}
