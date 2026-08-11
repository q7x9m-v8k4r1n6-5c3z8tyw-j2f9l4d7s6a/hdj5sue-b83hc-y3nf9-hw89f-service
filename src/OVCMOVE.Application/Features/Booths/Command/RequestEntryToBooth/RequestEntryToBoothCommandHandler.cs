using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Application.Features.Booths.Common;

namespace OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;

public class RequestEntryToBoothCommandHandler
    : IRequestHandler<RequestEntryToBoothCommand, (bool IsSuccess, string Message)>
{
    private readonly IBoothRepository _boothRepository;
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothNotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public RequestEntryToBoothCommandHandler(
        IBoothRepository boothRepository,
        IRaceRepository raceRepository,
        IBoothNotificationService notificationService,
        IUserRepository userRepository)
    {
        _boothRepository = boothRepository;
        _raceRepository = raceRepository;
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

        if (booth.Status != BoothConstants.BoothStatus.Free ||
            booth.TeamId is not null)
        {
            return (false, "Trạm thi đấu đang có đội khác sử dụng.");
        }

        var progress = await _raceRepository.GetBoothProgressAsync(
            booth.RaceId,
            request.TeamId,
            request.BoothId,
            cancellationToken);
        var entryError = BoothParticipationPolicy.GetEntryError(booth, progress);
        if (entryError is not null)
        {
            return (false, entryError);
        }

        var requested = await _boothRepository.TryRequestEntryAsync(
            request.BoothId,
            request.TeamId,
            cancellationToken);
        if (!requested)
        {
            return (false, "Trạm đang có yêu cầu khác hoặc đội đang ở trạm khác.");
        }

        var teamUser = await _userRepository.GetByIdAsync(request.TeamId, cancellationToken);
        var teamName = !string.IsNullOrWhiteSpace(teamUser?.DisplayName)
            ? teamUser.DisplayName
            : "Đội chưa đặt tên";

        await _notificationService.NotifyBoothStatusChangedAsync(
            booth.RaceId,
            request.BoothId,
            BoothConstants.BoothStatus.Pending,
            request.TeamId,
            teamName,
            cancellationToken);

        return (true, "Đã gửi yêu cầu vào trạm. Vui lòng chờ Ban tổ chức xác nhận!");
    }
}
