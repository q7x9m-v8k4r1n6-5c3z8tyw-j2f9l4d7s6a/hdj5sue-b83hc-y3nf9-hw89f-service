namespace OVCMOVE.Application.Abstractions.Plugins;

public static class PluginEventNames
{
    public const string BoothEntryRequested = "booth.entry.requested";
    public const string BoothResultFinalized = "booth.result.finalized";
}

public static class BoothResultValues
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}

/// <summary>
/// Core-owned snapshot of one booth result. Optional plugins may react to it,
/// but core remains the owner of booth lifecycle and the submitted score.
/// </summary>
public sealed class BoothResultFinalizedData
{
    public required Guid BoothCompletionId { get; init; }
    public required string BoothType { get; init; }
    public int? BoothMaximumScore { get; init; }
    public required int SubmittedPoints { get; init; }
    public required string Result { get; init; }
    public int FinalAwardedPoints { get; set; }
    public IList<PluginScoreAdjustment> ScoreAdjustments { get; } = [];
}

public sealed record PluginScoreAdjustment(Guid TeamId, int Delta);

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
    string EventId,
    BoothResultFinalizedData? BoothResult = null);

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
