namespace OVCMOVE.Application.Features.Booths.Common;

/// <summary>Core-owned finalized booth outcome used by optional gameplay plugins.</summary>
public sealed record FinalizedBoothOutcome(
    Guid TeamId,
    Guid BoothId,
    int SubmittedPoints,
    DateTime FinalizedAt);
