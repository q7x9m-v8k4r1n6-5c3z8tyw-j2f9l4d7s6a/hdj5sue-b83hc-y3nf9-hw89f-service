using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Application.Features.Booths.Common;

namespace OVCMOVE.Application.Features.Booths.Commands.AcceptEntryToBooth;

public class AcceptEntryToBoothCommandHandler
    : IRequestHandler<AcceptEntryToBoothCommand, (bool IsSuccess, string Message)>
{
    private readonly IBoothRepository _boothRepository;
    private readonly IBoothNotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothOrganizerRepository _boothOrganizerRepository;

    public AcceptEntryToBoothCommandHandler(
        IBoothRepository boothRepository,
        IBoothNotificationService notificationService,
        IUserRepository userRepository,
        IRaceRepository raceRepository,
        IBoothOrganizerRepository boothOrganizerRepository)
    {
        _boothRepository = boothRepository;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _raceRepository = raceRepository;
        _boothOrganizerRepository = boothOrganizerRepository;
    }

    public async Task<(bool IsSuccess, string Message)> Handle(
        AcceptEntryToBoothCommand request,
        CancellationToken cancellationToken)
    {
        var booth = await _boothRepository.GetByIdAsync(request.BoothId, cancellationToken);
        if (booth == null)
        {
            return (false, "Trạm thi đấu không tồn tại.");
        }

        var isAssigned = await _boothOrganizerRepository.IsAssignedAsync(
            request.OrganizerId,
            request.BoothId,
            cancellationToken);
        if (!isAssigned)
        {
            return (false, "Bạn không được phân công quản lý trạm này.");
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

        var isOccupied = await _boothRepository.TryOccupyAsync(
            request.BoothId,
            request.TeamId,
            cancellationToken);
        if (!isOccupied)
        {
            return (false, "Trạm đang được sử dụng hoặc đội đang ở trạm khác.");
        }

        var teamUser = await _userRepository.GetByIdAsync(request.TeamId, cancellationToken);
        var teamName = !string.IsNullOrWhiteSpace(teamUser?.DisplayName)
            ? teamUser.DisplayName
            : "Đội chưa đặt tên";

        await _notificationService.NotifyBoothStatusChangedAsync(
            booth.RaceId,
            request.BoothId,
            BoothConstants.BoothStatus.Occupied,
            request.TeamId,
            teamName,
            cancellationToken);

        return (true, "Đã chấp nhận cho đội vào trạm thành công!");
    }
}
