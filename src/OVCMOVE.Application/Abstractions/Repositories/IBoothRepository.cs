using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

/// <summary>
/// Repository quản lý truy xuất dữ liệu cho Entity Booth.
/// </summary>
public interface IBoothRepository
{
    // Đảm bảo CreateAsync trả về Task<Guid>
    Task<Guid> CreateAsync(Booth booth, CancellationToken cancellationToken = default);
    Task<Booth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Booth booth, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xử lý chấm điểm cho Đội chơi và giải phóng trạng thái Trạm.
    /// </summary>
    Task<bool> SubmitScoreAndReleaseAsync(
        Guid boothId,
        Guid teamId,
        Guid organizerId,
        int score,
        CancellationToken cancellationToken = default);
}