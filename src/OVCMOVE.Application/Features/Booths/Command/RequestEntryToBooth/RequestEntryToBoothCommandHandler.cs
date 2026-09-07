using MediatR;
using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Abstractions.Plugins;
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
    private readonly IPluginHub _pluginHub;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestEntryToBoothCommandHandler> _logger;

    public RequestEntryToBoothCommandHandler(
        IBoothRepository boothRepository,
        IRaceRepository raceRepository,
        IBoothNotificationService notificationService,
        IUserRepository userRepository,
        IPluginHub pluginHub,
        IUnitOfWork unitOfWork,
        ILogger<RequestEntryToBoothCommandHandler> logger)
    {
        _boothRepository = boothRepository;
        _raceRepository = raceRepository;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _pluginHub = pluginHub;
        _unitOfWork = unitOfWork;
        _logger = logger;
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

        IPluginEventExecution pluginExecution = NoopPluginEventExecution.Instance;
        var commitStarted = false;
        var occurredAt = DateTime.UtcNow;
        var eventId = $"booth-entry:{request.BoothId:N}:{request.TeamId:N}:{Guid.NewGuid():N}";
        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            var requested = await _boothRepository.TryRequestEntryAsync(
                request.BoothId,
                request.TeamId,
                cancellationToken);
            if (!requested)
            {
                await _unitOfWork.RollbackAsync(CancellationToken.None);
                return (false, "Trạm đang có yêu cầu khác hoặc đội đang ở trạm khác.");
            }

            pluginExecution = await _pluginHub.DispatchAsync(
                new PluginEventContext(
                    PluginEventNames.BoothEntryRequested,
                    booth.RaceId,
                    request.TeamId,
                    request.BoothId,
                    occurredAt,
                    eventId),
                cancellationToken);

            commitStarted = true;
            await _unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            if (!commitStarted)
                await pluginExecution.AbortAsync(CancellationToken.None);
            else
                _logger.LogCritical(
                    "SQL commit outcome is unknown for plugin event {EventId}; Mongo claim was retained to prevent replay.",
                    eventId);
            throw;
        }

        try
        {
            await pluginExecution.CompleteAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(
                exception,
                "SQL committed but plugin event {EventId} could not be completed. The Mongo claim requires reconciliation.",
                eventId);
        }

        var teamUser = await _userRepository.GetByIdAsync(request.TeamId, cancellationToken);
        var teamName = !string.IsNullOrWhiteSpace(teamUser?.DisplayName)
            ? teamUser.DisplayName
            : "Đội chưa đặt tên";

        foreach (var adjustment in pluginExecution.ScoreAdjustments)
        {
            await _notificationService.NotifyRaceScoreChangedAsync(
                booth.RaceId,
                adjustment.TeamId,
                adjustment.Delta,
                cancellationToken);
        }

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
