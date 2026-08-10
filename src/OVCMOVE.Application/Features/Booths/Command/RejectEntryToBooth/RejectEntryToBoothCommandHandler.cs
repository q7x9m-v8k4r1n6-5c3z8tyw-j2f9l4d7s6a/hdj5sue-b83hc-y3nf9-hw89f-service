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

        if (booth.Status != BoothConstants.BoothStatus.Pending ||
            booth.TeamId != request.TeamId)
        {
            throw new ApplicationConflictException(
                "Yêu cầu đã được xử lý hoặc không còn chờ duyệt.");
        }


        var rejected = await boothRepository.TryRejectEntryAsync(
            request.BoothId,
            request.TeamId,
            cancellationToken);
        if (!rejected)
        {
            throw new ApplicationConflictException(
                "Trạng thái yêu cầu đã thay đổi. Vui lòng thử lại.");
        }

        await notificationService.NotifyBoothEntryRejectedAsync(
            booth.RaceId,
            request.BoothId,
            request.TeamId,
            cancellationToken);

        await notificationService.NotifyBoothStatusChangedAsync(
            booth.RaceId,
            request.BoothId,
            BoothConstants.BoothStatus.Free,
            null,
            null,
            cancellationToken);
    }
}
