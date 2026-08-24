using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Application.Features.Workflows.Command;

public sealed class WorkflowRealtimePublisher(
    IBoothNotificationService notificationService,
    ILogger<WorkflowRealtimePublisher> logger)
{
    internal async Task<bool> PublishAsync(
        IReadOnlyCollection<WorkflowRealtimeEvent> events,
        CancellationToken cancellationToken)
    {
        var allSynchronized = true;
        foreach (var realtimeEvent in events)
        {
            if (!await PublishWithRetryAsync(realtimeEvent, cancellationToken))
                allSynchronized = false;
        }

        return allSynchronized;
    }

    private async Task<bool> PublishWithRetryAsync(
        WorkflowRealtimeEvent realtimeEvent,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= WorkflowRetryPolicy.MaximumAttempts; attempt++)
        {
            try
            {
                await PublishOnceAsync(realtimeEvent, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (attempt == WorkflowRetryPolicy.MaximumAttempts)
                {
                    logger.LogWarning(
                        exception,
                        "Không thể đồng bộ realtime của workflow sau {AttemptCount} lần thử.",
                        WorkflowRetryPolicy.MaximumAttempts);
                    return false;
                }

                await Task.Delay(
                    TimeSpan.FromMilliseconds(attempt * 150),
                    cancellationToken);
            }
        }

        return false;
    }

    private Task PublishOnceAsync(
        WorkflowRealtimeEvent realtimeEvent,
        CancellationToken cancellationToken) => realtimeEvent switch
        {
            WorkflowScoreChangedEvent scoreChanged =>
                notificationService.NotifyRaceScoreChangedAsync(
                    scoreChanged.RaceId,
                    scoreChanged.TeamId,
                    scoreChanged.Delta,
                    cancellationToken),
            WorkflowRaceMessageEvent raceMessage =>
                notificationService.NotifyRaceMessageAsync(
                    raceMessage.RaceId,
                    raceMessage.Message,
                    cancellationToken),
            _ => throw new InvalidOperationException(
                "Workflow realtime event không được hỗ trợ.")
        };
}
