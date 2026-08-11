using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Test.Application;

public sealed class BoothSessionQueryContractTests
{
    [Fact]
    public void GetActiveSession_IsScopedToTeamRaceAndActiveStatuses()
    {
        var query = BoothQueries.GetActiveBoothByTeamAndRaceQuery();

        Assert.Contains("[RaceID] = @RaceId", query, StringComparison.Ordinal);
        Assert.Contains("[TeamId] = @TeamId", query, StringComparison.Ordinal);
        Assert.Contains(
            "[Status] IN (@PendingStatus, @OccupiedStatus)",
            query,
            StringComparison.Ordinal);
        Assert.Contains("[IsDeleted] = 0", query, StringComparison.Ordinal);
        Assert.DoesNotContain("NOLOCK", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SessionTransitions_UseAtomicStateAndTeamGuards()
    {
        var requestEntry = BoothQueries.TryRequestBoothEntryQuery();
        var acceptEntry = BoothQueries.TryOccupyBoothQuery();
        var cancelSession = BoothQueries.TryReleaseBoothQuery();
        var submitScore = BoothQueries.UpdateTeamScoreQuery();

        Assert.Contains("[Status] = N'free'", requestEntry, StringComparison.Ordinal);
        Assert.Contains("[TeamId] IS NULL", requestEntry, StringComparison.Ordinal);

        Assert.Contains("[Status] = N'pending'", acceptEntry, StringComparison.Ordinal);
        Assert.Contains("[TeamId] = @TeamId", acceptEntry, StringComparison.Ordinal);

        Assert.Contains("[Status] = N'occupied'", cancelSession, StringComparison.Ordinal);
        Assert.Contains("[TeamId] = @TeamId", cancelSession, StringComparison.Ordinal);

        Assert.Contains("b.Status = N'occupied'", submitScore, StringComparison.Ordinal);
        Assert.Contains("b.TeamId = @TeamId", submitScore, StringComparison.Ordinal);
    }
}
