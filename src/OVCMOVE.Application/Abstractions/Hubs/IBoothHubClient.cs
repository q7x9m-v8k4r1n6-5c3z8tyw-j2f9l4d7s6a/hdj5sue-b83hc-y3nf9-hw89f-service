namespace OVCMOVE.Application.Abstractions.Hubs;

public interface IBoothHubClient
{
    /// <summary>
    /// Phát sự kiện đổi trạng thái trạm về cho tất cả App/Web đang xem bản đồ
    /// </summary>
    Task ReceiveBoothStatusChanged(Guid boothId, string status, Guid? teamId);
}