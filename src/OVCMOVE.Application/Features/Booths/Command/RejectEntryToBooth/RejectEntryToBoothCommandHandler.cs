using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Booths.Commands.RejectEntryToBooth;

public sealed class RejectEntryToBoothCommandHandler(
    IBoothRepository boothRepository,
    IBoothOrganizerRepository boothOrganizerRepository,
    IBoothNotificationService notificationService)
    : IRequestHandler<RejectEntryToBoothCommand>
{
    public async Task Handle(
        RejectEntryToBoothCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var booth = await boothRepository.GetByIdAsync(
            request.BoothId,
            cancellationToken)
            ?? throw new ApplicationNotFoundException(
                "Trạm thi đấu không tồn tại.");

        var isAssigned = await boothOrganizerRepository.IsAssignedAsync(
            request.OrganizerId,
            request.BoothId,
            cancellationToken);
        if (!isAssigned)
        {
            throw new ApplicationForbiddenException(
                "Bạn không được phân công quản lý trạm này.");
        }

        if (booth.Status == BoothConstants.BoothStatus.Occupied ||
            booth.TeamId is not null)
        {
            throw new ApplicationConflictException(
                "Yêu cầu đã được xử lý hoặc trạm đang có đội sử dụng.");
        }

        await notificationService.NotifyBoothEntryRejectedAsync(
            booth.RaceId,
            request.BoothId,
            request.TeamId,
            cancellationToken);
    }
}
