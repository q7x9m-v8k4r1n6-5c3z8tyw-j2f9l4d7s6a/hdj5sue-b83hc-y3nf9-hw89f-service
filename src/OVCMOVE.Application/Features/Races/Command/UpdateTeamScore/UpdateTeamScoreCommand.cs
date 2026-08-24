using MediatR;
using OVCMOVE.Application.Common;

namespace OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;

public sealed class UpdateTeamScoreCommand :
    AuditedRequest,
    IRequest<UpdateTeamScoreResult?>
{
    public Guid RaceId { get; init; }
    public Guid TeamId { get; init; }
    public int Delta { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool PublishRealtimeNotification { get; init; } = true;
}

public sealed record UpdateTeamScoreResult(
    Guid RaceId,
    Guid TeamId,
    int ScoreBefore,
    int ScoreAfter,
    int Delta);
