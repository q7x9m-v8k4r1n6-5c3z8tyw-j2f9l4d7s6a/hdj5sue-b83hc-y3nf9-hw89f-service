using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Plugins;

namespace OVCMOVE2026.Plugin.Services;

public interface IPluginEventHandler
{
    string EventName { get; }
    Task HandleAsync(PluginEventContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Dispatches core events to handlers owned by this optional plugin. A handler
/// failure is logged and swallowed so plugin behavior is never allowed to take
/// down the core request pipeline.
/// </summary>
public sealed class Move2026PluginHub(
    IEnumerable<IPluginEventHandler> handlers,
    ILogger<Move2026PluginHub> logger) : IPluginHub
{
    public async Task DispatchAsync(
        PluginEventContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var handler in handlers.Where(item =>
                     string.Equals(item.EventName, context.Name, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                await handler.HandleAsync(context, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Optional plugin handler {Handler} failed for event {EventName}; core request continues.",
                    handler.GetType().FullName,
                    context.Name);
            }
        }
    }
}

public sealed class TrapBoothEntryRequestedHandler(
    IRaceCardService cardService,
    MediatR.ISender sender,
    Microsoft.Extensions.Logging.ILogger<TrapBoothEntryRequestedHandler> logger) : IPluginEventHandler
{
    public string EventName => PluginEventNames.BoothEntryRequested;

    public async Task HandleAsync(
        PluginEventContext context,
        CancellationToken cancellationToken)
    {
        if (!context.BoothId.HasValue) return;
        var trap = await cardService.TriggerTrapAsync(
            context.RaceId,
            context.BoothId.Value,
            context.TeamId,
            context.OccurredAt,
            context.Name,
            context.EventId,
            cancellationToken);
        if (trap is null) return;

        var penaltyPoints = trap.Data.TryGetValue("penaltyPoints", out var configuredPenalty) &&
                            configuredPenalty.IsNumeric
            ? configuredPenalty.ToInt32()
            : 10;

        var score = await sender.Send(new OVCMOVE.Application.Features.Races.Command.UpdateTeamScore.UpdateTeamScoreCommand
        {
            RaceId = context.RaceId,
            TeamId = context.TeamId,
            Delta = -penaltyPoints,
            Reason = $"Trap tại trạm {context.BoothId.Value:N}",
            PublishRealtimeNotification = true
        }, cancellationToken);

        if (score is null)
        {
            logger.LogWarning(
                "Trap was triggered but team {TeamId} was not found in race {RaceId}.",
                context.TeamId,
                context.RaceId);
        }
    }
}
