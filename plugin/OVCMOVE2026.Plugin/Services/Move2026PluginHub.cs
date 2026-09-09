using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OVCMOVE.Application.Abstractions.Plugins;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public interface IPluginEventHandler
{
    string EventName { get; }
    Task<IPluginEventExecution?> PrepareAsync(
        PluginEventContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Dispatches core events to handlers owned by this optional plugin. Installed
/// gameplay handlers are fail-fast: a broken card operation must not silently
/// let the core gameplay operation commit.
/// </summary>
public sealed class Move2026PluginHub(
    IEnumerable<IPluginEventHandler> handlers,
    ILogger<Move2026PluginHub> logger) : IPluginHub
{
    public async Task<IPluginEventExecution> DispatchAsync(
        PluginEventContext context,
        CancellationToken cancellationToken = default)
    {
        var executions = new List<IPluginEventExecution>();
        try
        {
            foreach (var handler in handlers.Where(item =>
                         string.Equals(item.EventName, context.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var execution = await handler.PrepareAsync(context, cancellationToken);
                if (execution is not null)
                    executions.Add(execution);
            }

            return executions.Count == 0
                ? NoopPluginEventExecution.Instance
                : new CompositePluginEventExecution(executions, logger, context);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AbortPreparedAsync(executions);
            throw;
        }
        catch (Exception exception)
        {
            await AbortPreparedAsync(executions);
            logger.LogError(
                exception,
                "Gameplay plugin preparation failed for event {EventName}; core transaction will roll back.",
                context.Name);
            throw;
        }
    }

    private static async Task AbortPreparedAsync(IEnumerable<IPluginEventExecution> executions)
    {
        foreach (var execution in executions.Reverse())
        {
            try
            {
                await execution.AbortAsync(CancellationToken.None);
            }
            catch
            {
                // Preserve the original preparation failure. The claim remains
                // visible in Mongo for explicit reconciliation instead of replay.
            }
        }
    }
}

internal sealed class CompositePluginEventExecution(
    IReadOnlyCollection<IPluginEventExecution> executions,
    ILogger logger,
    PluginEventContext context) : IPluginEventExecution
{
    public IReadOnlyCollection<PluginScoreAdjustment> ScoreAdjustments { get; } =
        executions.SelectMany(item => item.ScoreAdjustments).ToArray();

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var execution in executions)
            await RetryAsync(execution.CompleteAsync, "complete", cancellationToken);
    }

    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        foreach (var execution in executions.Reverse())
            await RetryAsync(execution.AbortAsync, "abort", cancellationToken);
    }

    private async Task RetryAsync(
        Func<CancellationToken, Task> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await operation(cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastError = exception;
                logger.LogWarning(
                    exception,
                    "Plugin event {EventId} failed to {Operation} on attempt {Attempt}/3.",
                    context.EventId,
                    operationName,
                    attempt);
                if (attempt < 3)
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Plugin event '{context.EventId}' could not {operationName} after 3 attempts.",
            lastError);
    }
}

internal sealed class DeferredPluginEventExecution(
    Func<CancellationToken, Task> complete,
    Func<CancellationToken, Task> abort,
    IReadOnlyCollection<PluginScoreAdjustment>? scoreAdjustments = null) : IPluginEventExecution
{
    public IReadOnlyCollection<PluginScoreAdjustment> ScoreAdjustments { get; } =
        scoreAdjustments ?? [];

    public Task CompleteAsync(CancellationToken cancellationToken = default) => complete(cancellationToken);
    public Task AbortAsync(CancellationToken cancellationToken = default) => abort(cancellationToken);
}

public sealed class TrapBoothEntryRequestedHandler(
    IRaceCardService cardService,
    IRaceCardRepository repository,
    ISender sender) : IPluginEventHandler
{
    public string EventName => PluginEventNames.BoothEntryRequested;

    public async Task<IPluginEventExecution?> PrepareAsync(
        PluginEventContext context,
        CancellationToken cancellationToken)
    {
        if (!context.BoothId.HasValue) return null;
        try
        {
            var trap = await cardService.TriggerTrapAsync(
                context.RaceId,
                context.BoothId.Value,
                context.TeamId,
                context.OccurredAt,
                context.Name,
                context.EventId,
                cancellationToken);
            if (trap is null) return null;

            var penaltyPoints = trap.Data.TryGetValue("penaltyPoints", out var configuredPenalty) &&
                                configuredPenalty.IsNumeric
                ? configuredPenalty.ToInt32()
                : 10;
            var score = await sender.Send(new UpdateTeamScoreCommand
            {
                RaceId = context.RaceId,
                TeamId = context.TeamId,
                EventId = context.EventId,
                Delta = -penaltyPoints,
                Reason = $"Trap tại trạm {context.BoothId.Value:N}",
                PublishRealtimeNotification = false
            }, cancellationToken);
            if (score is null)
                throw new ApplicationValidationException(
                    "Không tìm thấy đội kích hoạt Trap trong race.");

            var resolution = new CardEffectResolution(
                trap.Id,
                "triggered",
                new BsonDocument
                {
                    ["boothId"] = context.BoothId.Value.ToString(),
                    ["targetTeamId"] = context.TeamId.ToString(),
                    ["penaltyPoints"] = penaltyPoints,
                    ["resolvedByEventId"] = context.EventId
                });
            return new DeferredPluginEventExecution(
                token => repository.CompleteClaimedEffectsAsync(
                    context.RaceId,
                    context.Name,
                    context.EventId,
                    context.TeamId,
                    context.OccurredAt,
                    [resolution],
                    token),
                token => repository.ReleaseClaimedEffectsAsync(
                    context.RaceId,
                    context.EventId,
                    DateTime.UtcNow,
                    token),
                [new PluginScoreAdjustment(context.TeamId, -penaltyPoints)]);
        }
        catch
        {
            await repository.ReleaseClaimedEffectsAsync(
                context.RaceId,
                context.EventId,
                DateTime.UtcNow,
                CancellationToken.None);
            throw;
        }
    }
}

public sealed class TaxmanBoothEntryRequestedHandler(
    IRaceCardRepository repository,
    IRaceRepository raceRepository,
    ISender sender) : IPluginEventHandler
{
    public string EventName => PluginEventNames.BoothEntryRequested;

    public async Task<IPluginEventExecution?> PrepareAsync(
        PluginEventContext context,
        CancellationToken cancellationToken)
    {
        if (!context.BoothId.HasValue) return null;
        var taxman = await repository.TryClaimTaxmanAsync(
            context.RaceId,
            context.BoothId.Value,
            context.TeamId,
            context.OccurredAt,
            context.Name,
            context.EventId,
            cancellationToken);
        if (taxman is null) return null;

        try
        {
            if (!Guid.TryParse(taxman.OwnerTeamId, out var ownerTeamId))
                throw new ApplicationValidationException("Taxman có ownerTeamId không hợp lệ.");
            var targetScore = await raceRepository.GetRaceTeamScoreAsync(
                context.RaceId,
                context.TeamId,
                cancellationToken)
                ?? throw new ApplicationValidationException("Không tìm thấy đội kích hoạt Taxman.");
            var configuredAmount = taxman.Data.GetInt("stealPoints", 20);
            var transferredAmount = Math.Min(Math.Max(0, targetScore), configuredAmount);
            var adjustments = new List<PluginScoreAdjustment>();

            if (transferredAmount > 0)
            {
                await AdjustAsync(
                    context,
                    context.TeamId,
                    -transferredAmount,
                    $"Taxman tại booth {context.BoothId.Value:D}",
                    cancellationToken);
                await AdjustAsync(
                    context,
                    ownerTeamId,
                    transferredAmount,
                    $"Nhận CD từ Taxman tại booth {context.BoothId.Value:D}",
                    cancellationToken);
                adjustments.Add(new PluginScoreAdjustment(context.TeamId, -transferredAmount));
                adjustments.Add(new PluginScoreAdjustment(ownerTeamId, transferredAmount));
            }

            var resolution = new CardEffectResolution(
                taxman.Id,
                "triggered",
                new BsonDocument
                {
                    ["boothId"] = context.BoothId.Value.ToString(),
                    ["targetTeamId"] = context.TeamId.ToString(),
                    ["configuredAmount"] = configuredAmount,
                    ["transferredAmount"] = transferredAmount,
                    ["resolvedByEventId"] = context.EventId
                },
                context.OccurredAt.AddMinutes(taxman.Data.GetInt("timeBetweenUseMinutes", 20)));
            return new DeferredPluginEventExecution(
                token => repository.CompleteClaimedEffectsAsync(
                    context.RaceId,
                    context.Name,
                    context.EventId,
                    context.TeamId,
                    context.OccurredAt,
                    [resolution],
                    token),
                token => repository.ReleaseClaimedEffectsAsync(
                    context.RaceId,
                    context.EventId,
                    DateTime.UtcNow,
                    token),
                adjustments);
        }
        catch
        {
            await repository.ReleaseClaimedEffectsAsync(
                context.RaceId,
                context.EventId,
                DateTime.UtcNow,
                CancellationToken.None);
            throw;
        }
    }

    private async Task AdjustAsync(
        PluginEventContext context,
        Guid teamId,
        int delta,
        string reason,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateTeamScoreCommand
        {
            RaceId = context.RaceId,
            TeamId = teamId,
            EventId = context.EventId,
            Delta = delta,
            Reason = reason,
            PublishRealtimeNotification = false
        }, cancellationToken);
        if (result is null)
            throw new ApplicationValidationException($"Không tìm thấy team '{teamId}' để xử lý Taxman.");
    }
}
