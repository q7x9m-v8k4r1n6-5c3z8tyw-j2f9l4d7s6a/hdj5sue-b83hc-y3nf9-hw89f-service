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
    private readonly IUserRepository _userRepository;

    public RequestEntryToBoothCommandHandler(
        IBoothRepository boothRepository,
        IBoothNotificationService notificationService,
        IUserRepository userRepository)
    {
        _boothRepository = boothRepository;
        _notificationService = notificationService;
        _userRepository = userRepository;
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

        var teamUser = await _userRepository.GetByIdAsync(request.TeamId, cancellationToken);
        var teamName = !string.IsNullOrWhiteSpace(teamUser?.DisplayName)
            ? teamUser.DisplayName
            : "Đội chưa đặt tên";

        await _notificationService.NotifyBoothStatusChangedAsync(
            booth.RaceId,
            request.BoothId,
            "Pending",
            request.TeamId,
            teamName,
            cancellationToken);

        return (true, "Đã gửi yêu cầu vào trạm. Vui lòng chờ Ban tổ chức xác nhận!");
    }
}