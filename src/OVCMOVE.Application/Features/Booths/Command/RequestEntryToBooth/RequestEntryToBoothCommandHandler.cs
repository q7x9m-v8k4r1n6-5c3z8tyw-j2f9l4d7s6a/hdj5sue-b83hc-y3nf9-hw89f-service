using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;

public class RequestEntryToBoothCommandHandler
    : IRequestHandler<RequestEntryToBoothCommand, (bool IsSuccess, string Message)>
{
    private const int RequiredNormalBoothsForHiddenBooth = 2;

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

        if (booth.Status == BoothConstants.BoothStatus.Occupied)
        {
            return (false, "Trạm thi đấu đang có đội khác sử dụng.");
        }

        var isTeamInRace = await _raceRepository.IsTeamInRaceAsync(
            booth.RaceId, request.TeamId, cancellationToken);
        if (!isTeamInRace)
        {
            return (false, "Đội của bạn không tham gia trận đấu này. Vui lòng kiểm tra lại mã QR.");
        }

        if (booth.IsHidden)
        {
            var completedNormalBooths = await _raceRepository.CountCompletedNormalBoothsAsync(
                booth.RaceId, request.TeamId, cancellationToken);
            if (completedNormalBooths < RequiredNormalBoothsForHiddenBooth)
            {
                return (false,
                    $"Đội bạn cần hoàn thành đủ {RequiredNormalBoothsForHiddenBooth} trạm thường trước khi vào trạm ẩn này. " +
                    $"Hiện đã hoàn thành: {completedNormalBooths}/{RequiredNormalBoothsForHiddenBooth}.");
            }
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