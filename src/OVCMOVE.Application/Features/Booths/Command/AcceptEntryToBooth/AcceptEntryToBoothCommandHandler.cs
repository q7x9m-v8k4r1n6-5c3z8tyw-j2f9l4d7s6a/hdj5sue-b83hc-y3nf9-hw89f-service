using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Booths.Commands.AcceptEntryToBooth;

public class AcceptEntryToBoothCommandHandler
    : IRequestHandler<AcceptEntryToBoothCommand, (bool IsSuccess, string Message)>
{
    private readonly IBoothRepository _boothRepository;
    private readonly IBoothNotificationService _notificationService;
    private readonly IUserRepository _userRepository;

    public AcceptEntryToBoothCommandHandler(
        IBoothRepository boothRepository,
        IBoothNotificationService notificationService,
        IUserRepository userRepository)
    {
        _boothRepository = boothRepository;
        _notificationService = notificationService;
        _userRepository = userRepository;
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

        booth.Status = BoothConstants.BoothStatus.Occupied;
        booth.TeamId = request.TeamId;

        var isUpdated = await _boothRepository.UpdateAsync(booth, cancellationToken);
        if (!isUpdated)
        {
            return (false, "Cập nhật trạng thái trạm thất bại.");
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