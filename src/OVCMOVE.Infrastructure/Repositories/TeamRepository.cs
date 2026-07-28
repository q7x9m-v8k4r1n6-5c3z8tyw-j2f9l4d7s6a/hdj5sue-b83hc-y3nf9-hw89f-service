using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

/// <summary>
/// Repository quản lý truy vấn dữ liệu Team từ Database sử dụng Dapper IDbExecutor.
/// </summary>
public class TeamRepository : ITeamRepository
{
    private readonly IDbExecutor _db;

    public TeamRepository(IDbExecutor db) =>
        _db = db;

    /// <summary>
    /// Lấy danh sách ID Team đã tồn tại trong hệ thống (dùng cho Validate).
    /// </summary>
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

    /// <summary>
    /// Lấy danh sách Team theo trang (Phân trang dưới SQL).
    /// </summary>
    public async Task<(IReadOnlyCollection<User> Items, int TotalItems)> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new
        {
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
            new { UserType = UserConstants.UserType.Team },
            cancellationToken: cancellationToken);

        return (result.ToArray(), totalItems);
    }

    /// <summary>
    /// Tìm kiếm Team theo từ khóa.
    /// </summary>
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
}