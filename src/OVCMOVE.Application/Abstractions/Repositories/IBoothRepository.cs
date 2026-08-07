using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

/// <summary>
/// Repository quản lý truy xuất dữ liệu cho Entity Booth.
/// </summary>
public interface IBoothRepository
{
    Task<Guid> CreateAsync(Booth booth, CancellationToken cancellationToken = default);
    Task<Booth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Booth booth, CancellationToken cancellationToken = default);
    Task<bool> TryOccupyAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default);
    Task<bool> TryReleaseAsync(
        Guid boothId,
        Guid teamId,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xử lý chấm điểm cho Đội chơi, ghi ScoringLog chi tiết và giải phóng trạng thái Trạm.
    /// </summary>
    Task<bool> SubmitScoreAndReleaseAsync(
    SubmitBoothScoreModel model,
    CancellationToken cancellationToken = default);
}
