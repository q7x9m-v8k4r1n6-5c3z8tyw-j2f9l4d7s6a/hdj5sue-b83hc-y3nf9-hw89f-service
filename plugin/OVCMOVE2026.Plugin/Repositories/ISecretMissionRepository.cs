using OVCMOVE2026.Plugin.Models;

namespace OVCMOVE2026.Plugin.Repositories;

public interface ISecretMissionRepository
{
    Task<SecretMission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateEvidenceAsync(SecretMission mission, CancellationToken cancellationToken = default);
    Task UpdateClaimAsync(SecretMission mission, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lấy danh sách các nhiệm vụ bí mật chưa được tạo mã QR (QrCodeUrl IS NULL)
    /// </summary>
    Task<IEnumerable<SecretMission>> GetMissionsWithoutQrCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật đường dẫn ảnh QR Code cho một nhiệm vụ cụ thể
    /// </summary>
    Task UpdateQrCodeUrlAsync(Guid id, string qrCodeUrl, CancellationToken cancellationToken = default);
}