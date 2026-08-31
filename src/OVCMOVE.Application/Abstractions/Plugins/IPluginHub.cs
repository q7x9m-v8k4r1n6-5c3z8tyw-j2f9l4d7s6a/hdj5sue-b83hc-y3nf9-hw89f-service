namespace OVCMOVE.Application.Abstractions.Plugins;

public static class PluginEventNames
{
    public const string BoothEntryRequested = "booth.entry.requested";
}

/// <summary>
/// Event data exposed by core to optional plugins. Core owns this contract so
/// removing a plugin assembly never creates a compile-time dependency.
/// </summary>
public sealed record PluginEventContext(
    string Name,
    Guid RaceId,
    Guid TeamId,
    Guid? BoothId,
    DateTime OccurredAt,
    string EventId);

public interface IPluginHub
{
    Task DispatchAsync(
        PluginEventContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>Safe default used when no optional plugin is installed.</summary>
public sealed class NoopPluginHub : IPluginHub
{
    public Task DispatchAsync(
        PluginEventContext context,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
