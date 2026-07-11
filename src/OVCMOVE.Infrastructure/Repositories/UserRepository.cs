using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;

namespace OVCMOVE.Infrastructure.Repositories;

public class UserRepository : BaseRepository<UserRepository>, IUserRepository
{
    public UserRepository(ILogger<UserRepository> logger, IDapperHelper dapperHelper) 
        : base(logger, dapperHelper)
    {
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM Users WHERE Username = @Username";
        return await _dapperHelper.QueryFirstOrDefaultAsync<UserEntity>(sql, new { Username = username });
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM Users WHERE Email = @Email";
        return await _dapperHelper.QueryFirstOrDefaultAsync<UserEntity>(sql, new { Email = email });
    }

    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        return await _dapperHelper.QueryFirstOrDefaultAsync<UserEntity>(sql, new { Id = id });
    }
}