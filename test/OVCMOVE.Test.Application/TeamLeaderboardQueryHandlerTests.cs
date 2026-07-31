using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Application.Features.Teams.Query.ScoreHistory;
using TeamLeaderboardFeature =
    OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public class TeamLeaderboardQueryHandlerTests
{
    [Theory]
    [InlineData(false, false, 0, null)]
    [InlineData(true, false, 2, 75)]
    [InlineData(true, true, 2, null)]
    public async Task Handle_AppliesRaceVisibilityRules(
        bool isLeaderboardVisible,
        bool hideOtherTeamPoints,
        int expectedTeamCount,
        int? expectedOtherTeamScore)
    {
        var currentTeamId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        var raceRepository = new RaceRepositoryStub(
            new Race
            {
                Id = raceId,
                IsToggledLeaderboard = isLeaderboardVisible,
                IsHiddenPoint = hideOtherTeamPoints
            },
            [
                new TeamLeaderboardResultModel
                {
                    TeamId = currentTeamId,
                    Rank = 1,
                    DisplayName = "Current",
                    TotalScore = 100
                },
                new TeamLeaderboardResultModel
                {
                    TeamId = otherTeamId,
                    Rank = 2,
                    DisplayName = "Other",
                    TotalScore = 75
                }
            ]);
        var handler = new TeamLeaderboardFeature.TeamLeaderboardQueryHandler(
            raceRepository,
            new ScoringLogRepositoryStub(
                new CompletedBoothStats(3, 2)));

        var result = await handler.Handle(
            new TeamLeaderboardFeature.TeamLeaderboardQuery(
                raceId,
                currentTeamId),
            CancellationToken.None);

        Assert.Equal(100, result.CurrentTeam.TotalScore);
        Assert.Equal(3, result.CurrentTeam.CompletedRegularBooths);
        Assert.Equal(2, result.CurrentTeam.CompletedHiddenBooths);
        Assert.Equal(expectedTeamCount, result.Teams.Count);

        if (isLeaderboardVisible)
        {
            Assert.Equal(
                100,
                Assert.Single(result.Teams, item => item.IsCurrentTeam)
                    .TotalScore);
            Assert.Equal(
                expectedOtherTeamScore,
                Assert.Single(result.Teams, item => !item.IsCurrentTeam)
                    .TotalScore);
        }
    }

    private sealed class RaceRepositoryStub(
        Race race,
        List<TeamLeaderboardResultModel> leaderboard)
        : IRaceRepository
    {
        public Task<Race?> GetByIdAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Race?>(race.Id == raceId ? race : null);

        public Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(
            Guid? raceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(leaderboard);

        public Task CreateAsync(
            Race race,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)>
            GetPageAsync(
                int page,
                int pageSize,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RaceDetailResultModel?> GetDetailAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            Race race,
            DateTime expectedModifiedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<BoothListResultModel>> GetBoothListAsync(
            Guid? raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(
            IReadOnlyCollection<ScoringLogResultModel> Items,
            int TotalItems)> GetScoringLogPageByRaceIdAsync(
                Guid raceId,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int?> GetRaceTeamScoreAsync(
            Guid raceId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateRaceTeamScoreAsync(
            Guid raceId,
            Guid teamId,
            int totalScore,
            string modifiedBy,
            DateTime modifiedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task CreateScoringLogAsync(
            ScoringLog log,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ScoringLogRepositoryStub(
        CompletedBoothStats stats) : IScoringLogRepository
    {
        public Task<CompletedBoothStats> GetCompletedBoothStatsAsync(
            Guid raceId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(stats);

        public Task<(
            IReadOnlyCollection<ScoreHistoryItemResultModel> Items,
            int TotalItems)> GetPageAsync(
                Guid raceId,
                Guid teamId,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
