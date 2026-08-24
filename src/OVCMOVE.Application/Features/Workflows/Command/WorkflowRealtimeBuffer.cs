using OVCMOVE.Application.Features.Races.Common;

namespace OVCMOVE.Application.Features.Workflows.Command;

internal abstract record WorkflowRealtimeEvent;

internal sealed record WorkflowScoreChangedEvent(
    Guid RaceId,
    Guid TeamId,
    int Delta) : WorkflowRealtimeEvent;

internal sealed record WorkflowRaceMessageEvent(
    Guid RaceId,
    RaceMessageResultModel Message) : WorkflowRealtimeEvent;

public sealed class WorkflowRealtimeBuffer
{
    private readonly List<WorkflowRealtimeEvent> _events = [];

    internal void EnqueueScoreChanged(Guid raceId, Guid teamId, int delta) =>
        _events.Add(new WorkflowScoreChangedEvent(raceId, teamId, delta));

    internal void EnqueueRaceMessage(Guid raceId, RaceMessageResultModel message) =>
        _events.Add(new WorkflowRaceMessageEvent(raceId, message));

    internal IReadOnlyCollection<WorkflowRealtimeEvent> Snapshot() =>
        _events.ToArray();

    internal void Reset() => _events.Clear();
}
