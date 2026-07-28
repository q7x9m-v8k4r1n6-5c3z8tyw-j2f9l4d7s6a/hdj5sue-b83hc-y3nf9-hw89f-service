using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

/// <summary>
/// Abstraction cho Repository quản lý dữ liệu Team/User ở tầng Domain/Application.
/// </summary>
public interface ITeamRepository
{
    /// <summary>
    /// Kiểm tra danh sách Team ID tồn tại trong hệ thống (dùng cho validation hàng loạt).
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IEnumerable<Guid> teamIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách phân trang.
    /// </summary>
    Task<(IReadOnlyCollection<User> Items, int TotalItems)> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm theo từ khóa.
    /// </summary>
    Task<IReadOnlyCollection<User>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default);
}