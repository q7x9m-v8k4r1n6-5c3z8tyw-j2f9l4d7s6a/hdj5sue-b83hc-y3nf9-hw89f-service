using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Application.Features.Races.Query.GetRaceRules;
using OVCMOVE.Application.Features.Teams.Query.ScoreHistory;
using OVCMOVE.Application.Features.Booths.Common;
using TeamLeaderboardFeature =
    OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public class TeamLeaderboardQueryHandlerTests
{
    [Fact]
    public async Task RaceRules_TeamInRaceWithNullRules_ReturnsEmptyRules()
    {
        var raceId = Guid.NewGuid();
        var repository = new RaceRepositoryStub(
            new Race { Id = raceId },
            [],
            (0, 0),
            teamInRace: true,
            rules: null);
        var handler = new GetRaceRulesQueryHandler(repository);

        var result = await handler.Handle(
            new GetRaceRulesQuery
            {
                RaceId = raceId,
                TeamId = Guid.NewGuid()
            },
            CancellationToken.None);

        Assert.True(result.IsTeamInRace);
        Assert.Equal(string.Empty, result.Rules);
    }

    [Fact]
    public async Task RaceRules_TeamOutsideRace_IsReportedSeparately()
    {
        var raceId = Guid.NewGuid();
        var repository = new RaceRepositoryStub(
            new Race { Id = raceId },
            [],
            (0, 0),
            teamInRace: false,
            rules: "Rules");
        var handler = new GetRaceRulesQueryHandler(repository);

        var result = await handler.Handle(
            new GetRaceRulesQuery
            {
                RaceId = raceId,
                TeamId = Guid.NewGuid()
            },
            CancellationToken.None);

        Assert.False(result.IsTeamInRace);
        Assert.Equal(string.Empty, result.Rules);
    }

    [Fact]
    public async Task ScoreHistory_UsesCurrentTeamFilterAndMapsSharedAdminLog()
    {
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var boothId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var repository = new RaceRepositoryStub(
            new Race { Id = raceId },
            [],
            (0, 0),
            [
                new ScoringLogResultModel
                {
                    LogId = Guid.NewGuid(),
                    BoothId = boothId,
                    ActorId = organizerId,
                    EventCode = "BOOTH",
                    ReasonCode = "BOOTH_COMPLETED",
                    ScoreDelta = 30,
                    ScoreAfter = 130,
                    Reason = "Completed booth",
                    CreatedAt = DateTime.UtcNow
                }
            ],
            100);
        var handler = new ScoreHistoryQueryHandler(repository);

        var result = await handler.Handle(
            new ScoreHistoryQuery(raceId, teamId, 0, 500),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(teamId, repository.LastScoringLogTeamId);
        Assert.Equal(1, result.Page);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(boothId, item.BoothId);
        Assert.Equal(organizerId, item.OrganizerId);
        Assert.Equal(30, item.ScoreGiven);
        Assert.Equal(130, item.ScoreAfterChange);
        Assert.Equal("booth_completed", item.Source);
    }

    [Fact]
    public async Task ScoreHistory_RejectsTeamOutsideRace()
    {
        var raceId = Guid.NewGuid();
        var repository = new RaceRepositoryStub(
            new Race { Id = raceId },
            [],
            (0, 0),
            currentScore: null);
        var handler = new ScoreHistoryQueryHandler(repository);

        await Assert.ThrowsAsync<ApplicationNotFoundException>(() =>
            handler.Handle(
                new ScoreHistoryQuery(raceId, Guid.NewGuid()),
                CancellationToken.None));

        Assert.Null(repository.LastScoringLogTeamId);
    }

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
            ],
            (3, 2));
        var handler = new TeamLeaderboardFeature.TeamLeaderboardQueryHandler(
            raceRepository);

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
        List<TeamLeaderboardResultModel> leaderboard,
        (int CompletedRegularBooths, int CompletedHiddenBooths) stats,
        IReadOnlyCollection<ScoringLogResultModel>? scoringLogs = null,
        int? currentScore = 0,
        bool teamInRace = true,
        string? rules = "")
        : IRaceRepository
    {
        public Guid? LastScoringLogTeamId { get; private set; }

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
                RacePageRequestModel request,
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
                Guid? teamId,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            LastScoringLogTeamId = teamId;
            return Task.FromResult((
                scoringLogs ?? Array.Empty<ScoringLogResultModel>(),
                scoringLogs?.Count ?? 0));
        }

        public Task<(
            int CompletedRegularBooths,
            int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(
                Guid raceId,
                Guid teamId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(stats);

        public Task<int?> GetRaceTeamScoreAsync(
            Guid raceId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(currentScore);

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

        public Task CreateRaceMessageAsync(
            RaceMessage message,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(
            Guid raceId,
            int limit,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> IsTeamInRaceAsync(
            Guid raceId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(teamInRace);

        public Task<string?> GetRulesAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(rules);

        public Task<BoothProgressResultModel> GetBoothProgressAsync(
            Guid raceId,
            Guid teamId,
            Guid boothId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

}
