using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.GetRaceDetail;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class GetRaceDetailQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenAdminView_ReturnsOriginalBoothsUnchanged()
    {
        // Arrange: Tất cả các cờ ẩn/tắt bảo mật đều bật, nhưng request là Admin (IsParticipantView = false)
        var detail = CreateSampleRaceDetail(
            isShowHiddenBooths: false,
            isHideBoothDescription: true,
            isDisabledBoothStatus: true);

        var handler = new GetRaceDetailQueryHandler(
            new StubRaceRepository(detail),
            new StubRaceTeamRepository());

        // Act
        var result = await handler.Handle(
            new GetRaceDetailQuery
            {
                RaceId = detail.Id,
                IsParticipantView = false
            },
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Booth.Count);

        var regular = Assert.Single(result.Booth, b => !b.IsHidden);
        Assert.Equal("Solve puzzle A", regular.Description);
        Assert.Equal("free", regular.Status);

        var hidden = Assert.Single(result.Booth, b => b.IsHidden);
        Assert.Equal("Secret challenge", hidden.Description);
        Assert.Equal("pending", hidden.Status);
    }

    [Fact]
    public async Task Handle_WhenParticipantViewAndShowHiddenBoothsIsFalse_RemovesHiddenBooths()
    {
        // Arrange: IsParticipantView = true, IsShowHiddenBooths = false
        var detail = CreateSampleRaceDetail(
            isShowHiddenBooths: false,
            isHideBoothDescription: false,
            isDisabledBoothStatus: false);

        var handler = new GetRaceDetailQueryHandler(
            new StubRaceRepository(detail),
            new StubRaceTeamRepository());

        // Act
        var result = await handler.Handle(
            new GetRaceDetailQuery
            {
                RaceId = detail.Id,
                IsParticipantView = true
            },
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Booth);
        Assert.All(result.Booth, b => Assert.False(b.IsHidden));
    }

    [Fact]
    public async Task Handle_WhenParticipantViewAndShowHiddenBoothsIsTrue_IncludesHiddenBooths()
    {
        // Arrange: IsParticipantView = true, IsShowHiddenBooths = true
        var detail = CreateSampleRaceDetail(
            isShowHiddenBooths: true,
            isHideBoothDescription: false,
            isDisabledBoothStatus: false);

        var handler = new GetRaceDetailQueryHandler(
            new StubRaceRepository(detail),
            new StubRaceTeamRepository());

        // Act
        var result = await handler.Handle(
            new GetRaceDetailQuery
            {
                RaceId = detail.Id,
                IsParticipantView = true
            },
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Booth.Count);
        Assert.Contains(result.Booth, b => b.IsHidden);
    }

    [Fact]
    public async Task Handle_WhenParticipantViewAndHideBoothDescriptionIsTrue_ClearsBoothDescriptions()
    {
        // Arrange: IsParticipantView = true, IsHideBoothDescription = true
        var detail = CreateSampleRaceDetail(
            isShowHiddenBooths: true,
            isHideBoothDescription: true,
            isDisabledBoothStatus: false);

        var handler = new GetRaceDetailQueryHandler(
            new StubRaceRepository(detail),
            new StubRaceTeamRepository());

        // Act
        var result = await handler.Handle(
            new GetRaceDetailQuery
            {
                RaceId = detail.Id,
                IsParticipantView = true
            },
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Booth.Count);
        Assert.All(result.Booth, b => Assert.Equal(string.Empty, b.Description));
    }

    [Fact]
    public async Task Handle_WhenParticipantViewAndDisabledBoothStatusIsTrue_SetsBoothStatusToOccupied()
    {
        // Arrange: IsParticipantView = true, IsDisabledBoothStatus = true
        var detail = CreateSampleRaceDetail(
            isShowHiddenBooths: true,
            isHideBoothDescription: false,
            isDisabledBoothStatus: true);

        var handler = new GetRaceDetailQueryHandler(
            new StubRaceRepository(detail),
            new StubRaceTeamRepository());

        // Act
        var result = await handler.Handle(
            new GetRaceDetailQuery
            {
                RaceId = detail.Id,
                IsParticipantView = true
            },
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Booth.Count);
        Assert.All(result.Booth, b => Assert.Equal("occupied", b.Status));
    }

    [Fact]
    public async Task Handle_WhenTeamNotAssignedToRace_ReturnsNull()
    {
        var raceId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var detail = CreateSampleRaceDetail();

        var handler = new GetRaceDetailQueryHandler(
            new StubRaceRepository(detail),
            new StubRaceTeamRepository([Guid.NewGuid()]));

        var result = await handler.Handle(
            new GetRaceDetailQuery
            {
                RaceId = raceId,
                TeamId = teamId,
                IsParticipantView = true
            },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenRaceNotFound_ReturnsNull()
    {
        var handler = new GetRaceDetailQueryHandler(
            new StubRaceRepository(null),
            new StubRaceTeamRepository());

        var result = await handler.Handle(
            new GetRaceDetailQuery
            {
                RaceId = Guid.NewGuid(),
                IsParticipantView = true
            },
            CancellationToken.None);

        Assert.Null(result);
    }

    private static RaceDetailResultModel CreateSampleRaceDetail(
        bool isShowHiddenBooths = true,
        bool isHideBoothDescription = false,
        bool isDisabledBoothStatus = false)
    {
        var regularBooth = new RaceBoothModel
        {
            Id = Guid.NewGuid(),
            Name = "Regular Booth",
            Place = "Zone A",
            Description = "Solve puzzle A",
            IsHidden = false,
            Status = "free",
            Type = "intellectual",
            MaximumScore = 100,
            MapX = 10.5,
            MapY = 20.5,
            OrganizerIds = [Guid.NewGuid()]
        };

        var hiddenBooth = new RaceBoothModel
        {
            Id = Guid.NewGuid(),
            Name = "Hidden Secret Booth",
            Place = "Zone B",
            Description = "Secret challenge",
            IsHidden = true,
            Status = "pending",
            Type = "physical",
            MaximumScore = 200,
            MapX = 30.5,
            MapY = 40.5,
            OrganizerIds = [Guid.NewGuid()]
        };

        return new RaceDetailResultModel
        {
            Id = Guid.NewGuid(),
            Name = "OVC Marathon",
            RaceName = "OVC Marathon 2026",
            TimeStart = DateTime.UtcNow,
            TimeEnd = DateTime.UtcNow.AddHours(4),
            Place = "Tech Park",
            Status = "running",
            CoverUrl = "https://example.com/cover.jpg",
            MapImageUrl = "https://example.com/map.jpg",
            ModifiedAt = DateTime.UtcNow,
            IsToggledLeaderboard = true,
            IsHiddenPoint = false,
            IsShowHiddenBooths = isShowHiddenBooths,
            IsHideBoothDescription = isHideBoothDescription,
            IsDisabledBoothStatus = isDisabledBoothStatus,
            Booth = [regularBooth, hiddenBooth]
        };
    }

    private sealed class StubRaceRepository(RaceDetailResultModel? detail) : IRaceRepository
    {
        public Task<RaceDetailResultModel?> GetDetailAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(detail);

        public Task CreateAsync(Race race, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(RacePageRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Race race, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateMapImageUrlAsync(Guid raceId, string mapImageUrl, string? modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<BoothListResultModel>> GetBoothListAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<ScoringLogResultModel> Items, int TotalItems)> GetScoringLogPageByRaceIdAsync(Guid raceId, Guid? teamId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int CompletedRegularBooths, int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetRaceTeamScoreAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateRaceTeamScoreAsync(Guid raceId, Guid teamId, int totalScore, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OVCMOVE.Application.Features.Races.Common.RaceTeamScoreMutation?> TryDebitRaceTeamScoreAsync(Guid raceId, Guid teamId, int amount, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateScoringLogAsync(ScoringLog log, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ScoringLog>> GetScoringLogsByEventIdAsync(Guid raceId, string eventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateRaceMessageAsync(RaceMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(Guid raceId, int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsTeamInRaceAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BoothProgressResultModel> GetBoothProgressAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<FinalizedBoothOutcome>> GetFinalizedBoothOutcomesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubRaceTeamRepository(IReadOnlyCollection<Guid>? teamIds = null) : IRaceTeamRepository
    {
        public Task<IReadOnlyCollection<Guid>> GetTeamIdsByRaceIdAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>(teamIds ?? []);

        public Task CreateAsync(RaceTeam raceTeam, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
