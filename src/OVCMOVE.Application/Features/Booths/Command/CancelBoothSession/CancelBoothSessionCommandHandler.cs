using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Booths.Commands.CancelBoothSession;

public sealed class CancelBoothSessionCommandHandler(
    IBoothRepository boothRepository,
    IBoothOrganizerRepository boothOrganizerRepository,
    IBoothNotificationService notificationService)
    : IRequestHandler<CancelBoothSessionCommand>
{
    public async Task Handle(
        CancelBoothSessionCommand request,
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

        if (booth.Status != BoothConstants.BoothStatus.Occupied ||
            booth.TeamId is null)
        {
            throw new ApplicationConflictException(
                "Trạm hiện không có đội để hủy.");
        }

        var teamId = booth.TeamId.Value;
        var released = await boothRepository.TryReleaseAsync(
            request.BoothId,
            teamId,
            cancellationToken);
        if (!released)
        {
            throw new ApplicationConflictException(
                "Trạng thái trạm đã thay đổi. Vui lòng thử lại.");
        }

        await notificationService.NotifyBoothEntryCancelledAsync(
            booth.RaceId,
            request.BoothId,
            teamId,
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
