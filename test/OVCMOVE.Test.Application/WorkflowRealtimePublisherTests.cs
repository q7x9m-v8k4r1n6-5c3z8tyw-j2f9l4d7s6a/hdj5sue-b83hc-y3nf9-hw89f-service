using Microsoft.Extensions.Logging.Abstractions;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Workflows.Command;

namespace OVCMOVE.Test.Application;

public sealed class WorkflowRealtimePublisherTests
{
    [Fact]
    public async Task PublishAsync_retries_immediately_and_succeeds_on_third_attempt()
    {
        var notification = new NotificationServiceDouble(failuresBeforeSuccess: 2);
        var publisher = new WorkflowRealtimePublisher(
            notification,
            NullLogger<WorkflowRealtimePublisher>.Instance);
        var buffer = new WorkflowRealtimeBuffer();
        buffer.EnqueueScoreChanged(Guid.NewGuid(), Guid.NewGuid(), 10);

        var synchronized = await publisher.PublishAsync(
            buffer.Snapshot(),
            CancellationToken.None);

        Assert.True(synchronized);
        Assert.Equal(WorkflowRetryPolicy.MaximumAttempts, notification.ScoreAttempts);
    }

    [Fact]
    public async Task PublishAsync_returns_false_after_three_failures_without_throwing()
    {
        var notification = new NotificationServiceDouble(failuresBeforeSuccess: int.MaxValue);
        var publisher = new WorkflowRealtimePublisher(
            notification,
            NullLogger<WorkflowRealtimePublisher>.Instance);
        var buffer = new WorkflowRealtimeBuffer();
        buffer.EnqueueScoreChanged(Guid.NewGuid(), Guid.NewGuid(), 10);

        var synchronized = await publisher.PublishAsync(
            buffer.Snapshot(),
            CancellationToken.None);

        Assert.False(synchronized);
        Assert.Equal(WorkflowRetryPolicy.MaximumAttempts, notification.ScoreAttempts);
    }

    private sealed class NotificationServiceDouble(int failuresBeforeSuccess) :
        IBoothNotificationService
    {
        public int ScoreAttempts { get; private set; }

        public Task NotifyRaceScoreChangedAsync(
            Guid raceId,
            Guid teamId,
            int delta,
            CancellationToken cancellationToken = default)
        {
            ScoreAttempts++;
            if (ScoreAttempts <= failuresBeforeSuccess)
                throw new InvalidOperationException("SignalR unavailable.");

            return Task.CompletedTask;
        }

        public Task NotifyBoothStatusChangedAsync(Guid raceId, Guid boothId, string status, Guid? teamId, string? teamName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyBoothEntryCancelledAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyBoothEntryRejectedAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceMessageAsync(Guid raceId, RaceMessageResultModel message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
