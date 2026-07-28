using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;

public class RequestEntryToBoothCommandHandler
    : IRequestHandler<RequestEntryToBoothCommand, (bool IsSuccess, string Message)>
{
    private readonly IBoothRepository _boothRepository;
    private readonly IBoothNotificationService _notificationService;

    public RequestEntryToBoothCommandHandler(
        IBoothRepository boothRepository,
        IBoothNotificationService notificationService)
    {
        _boothRepository = boothRepository;
        _notificationService = notificationService;
    }

    public async Task<(bool IsSuccess, string Message)> Handle(
        RequestEntryToBoothCommand request,
        CancellationToken cancellationToken)
    {
        var booth = await _boothRepository.GetByIdAsync(request.BoothId, cancellationToken);
        if (booth == null)
        {
            return (false, "Trạm thi đấu không tồn tại.");
        }

        if (booth.Status == BoothConstants.BoothStatus.Occupied)
        {
            return (false, "Trạm thi đấu đang có đội khác sử dụng.");
        }

        booth.Status = BoothConstants.BoothStatus.Occupied;
        var isUpdated = await _boothRepository.UpdateAsync(booth, cancellationToken);
        if (!isUpdated)
        {
            return (false, "Cập nhật trạng thái trạm thất bại.");
        }

        await _notificationService.NotifyBoothStatusChangedAsync(
            booth.RaceId,
            request.BoothId,
            BoothConstants.BoothStatus.Occupied,
            request.TeamId,
            cancellationToken);

        return (true, "Yêu cầu vào trạm thành công.");
    }
}