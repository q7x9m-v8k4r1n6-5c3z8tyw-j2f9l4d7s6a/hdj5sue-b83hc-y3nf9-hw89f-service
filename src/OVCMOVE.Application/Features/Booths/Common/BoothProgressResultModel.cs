namespace OVCMOVE.Application.Features.Booths.Common;

public sealed record BoothProgressResultModel
{
    public bool IsTeamInRace { get; init; }
    public bool HasCompletedBooth { get; init; }
    public int CompletedRegularBooths { get; init; }
    public int CompletedHiddenBooths { get; init; }
}
