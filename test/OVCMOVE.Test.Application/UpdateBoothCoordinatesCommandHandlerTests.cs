using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Races.Command.UpdateBoothCoordinates;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Api.Contracts;

namespace OVCMOVE.Test.Application;

public class UpdateBoothCoordinatesCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_UpdatesCoordinatesWithinTransaction()
    {
        var raceId = Guid.NewGuid();
        var booth1 = new Booth { Id = Guid.NewGuid(), RaceId = raceId, Name = "Trạm 1" };
        var booth2 = new Booth { Id = Guid.NewGuid(), RaceId = raceId, Name = "Trạm 2" };

        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Ongoing };
        var raceRepo = new TestRaceRepository(race);
        var boothRepo = new TestBoothRepository([booth1, booth2]);
        var unitOfWork = new TestUnitOfWork();
        var handler = new UpdateBoothCoordinatesCommandHandler(raceRepo, boothRepo, unitOfWork);

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates =
            [
                new BoothCoordinateItemModel { BoothId = booth1.Id, MapX = 12.5, MapY = 34.2 },
                new BoothCoordinateItemModel { BoothId = booth2.Id, MapX = 99.9, MapY = 0.0 }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        Assert.True(unitOfWork.BeginCalled);
        Assert.True(unitOfWork.CommitCalled);
        Assert.False(unitOfWork.RollbackCalled);
        Assert.Equal(2, boothRepo.UpdatedBatches.Count);
        Assert.Equal(12.5, booth1.MapX);
        Assert.Equal(34.2, booth1.MapY);
        Assert.Equal(99.9, booth2.MapX);
        Assert.Equal(0.0, booth2.MapY);
    }

    [Fact]
    public async Task Handle_EmptyCoordinates_ReturnsTrueWithoutTransaction()
    {
        var raceId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Ongoing };
        var raceRepo = new TestRaceRepository(race);
        var boothRepo = new TestBoothRepository([]);
        var unitOfWork = new TestUnitOfWork();
        var handler = new UpdateBoothCoordinatesCommandHandler(raceRepo, boothRepo, unitOfWork);

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates = []
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        Assert.False(unitOfWork.BeginCalled);
    }

    [Fact]
    public async Task Handle_EmptyRaceId_ThrowsValidationException()
    {
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(null),
            new TestBoothRepository([]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = Guid.Empty,
            Coordinates = []
        };

        var ex = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("RaceId", ex.Message);
    }

    [Fact]
    public async Task Handle_RaceNotFound_ThrowsNotFoundException()
    {
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(null),
            new TestBoothRepository([]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = Guid.NewGuid(),
            Coordinates = []
        };

        await Assert.ThrowsAsync<ApplicationNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RaceCompleted_ThrowsConflictException()
    {
        var raceId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Completed };
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(race),
            new TestBoothRepository([]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates = []
        };

        var ex = await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("kết thúc", ex.Message);
    }

    [Fact]
    public async Task Handle_NullCoordinates_ThrowsValidationException()
    {
        var raceId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Draft };
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(race),
            new TestBoothRepository([]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates = null!
        };

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyBoothId_ThrowsValidationException()
    {
        var raceId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Ongoing };
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(race),
            new TestBoothRepository([]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates =
            [
                new BoothCoordinateItemModel { BoothId = Guid.Empty, MapX = 50.0, MapY = 50.0 }
            ]
        };

        var ex = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("BoothId", ex.Message);
    }

    [Theory]
    [InlineData(-0.1, 50.0)]
    [InlineData(100.1, 50.0)]
    [InlineData(50.0, -1.0)]
    [InlineData(50.0, 105.0)]
    [InlineData(double.NaN, 50.0)]
    [InlineData(50.0, double.PositiveInfinity)]
    public async Task Handle_InvalidCoordinateRange_ThrowsValidationException(double mapX, double mapY)
    {
        var raceId = Guid.NewGuid();
        var boothId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Ongoing };
        var booth = new Booth { Id = boothId, RaceId = raceId };
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(race),
            new TestBoothRepository([booth]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates =
            [
                new BoothCoordinateItemModel { BoothId = boothId, MapX = mapX, MapY = mapY }
            ]
        };

        var ex = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("khoảng từ 0.0 đến 100.0", ex.Message);
    }

    [Fact]
    public async Task Handle_DuplicateBoothIds_ThrowsValidationException()
    {
        var raceId = Guid.NewGuid();
        var boothId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Ongoing };
        var booth = new Booth { Id = boothId, RaceId = raceId };
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(race),
            new TestBoothRepository([booth]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates =
            [
                new BoothCoordinateItemModel { BoothId = boothId, MapX = 10.0, MapY = 20.0 },
                new BoothCoordinateItemModel { BoothId = boothId, MapX = 30.0, MapY = 40.0 }
            ]
        };

        var ex = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("trùng lặp", ex.Message);
    }

    [Fact]
    public async Task Handle_BoothDoesNotBelongToRace_ThrowsValidationException()
    {
        var raceId = Guid.NewGuid();
        var foreignBoothId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Ongoing };
        var localBooth = new Booth { Id = Guid.NewGuid(), RaceId = raceId };
        var handler = new UpdateBoothCoordinatesCommandHandler(
            new TestRaceRepository(race),
            new TestBoothRepository([localBooth]),
            new TestUnitOfWork());

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates =
            [
                new BoothCoordinateItemModel { BoothId = foreignBoothId, MapX = 50.0, MapY = 50.0 }
            ]
        };

        var ex = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("không thuộc về giải đấu", ex.Message);
    }

    [Fact]
    public async Task Handle_PersistenceFailure_RollsBackTransaction()
    {
        var raceId = Guid.NewGuid();
        var boothId = Guid.NewGuid();
        var race = new Race { Id = raceId, Status = RaceConstants.RaceStatus.Ongoing };
        var booth = new Booth { Id = boothId, RaceId = raceId };
        var raceRepo = new TestRaceRepository(race);
        var boothRepo = new TestBoothRepository([booth]) { ThrowOnUpdate = true };
        var unitOfWork = new TestUnitOfWork();
        var handler = new UpdateBoothCoordinatesCommandHandler(raceRepo, boothRepo, unitOfWork);

        var command = new UpdateBoothCoordinatesCommand
        {
            RaceId = raceId,
            Coordinates =
            [
                new BoothCoordinateItemModel { BoothId = boothId, MapX = 50.0, MapY = 50.0 }
            ]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.True(unitOfWork.BeginCalled);
        Assert.False(unitOfWork.CommitCalled);
        Assert.True(unitOfWork.RollbackCalled);
    }

    [Fact]
    public void UpdateBoothCoordinatesRequest_DeserializesFromJsonArray()
    {
        var boothId = Guid.NewGuid();
        var json = $@"[{{""boothId"":""{boothId}"",""mapX"":12.3,""mapY"":45.6}}]";

        var request = System.Text.Json.JsonSerializer.Deserialize<RaceContract.UpdateBoothCoordinatesRequest>(
            json,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        var item = Assert.Single(request.Coordinates);
        Assert.Equal(boothId, item.BoothId);
        Assert.Equal(12.3, item.MapX);
        Assert.Equal(45.6, item.MapY);
    }

    [Fact]
    public void UpdateBoothCoordinatesRequest_DeserializesFromJsonObject()
    {
        var boothId = Guid.NewGuid();
        var json = $@"{{""coordinates"":[{{""boothId"":""{boothId}"",""mapX"":65.4,""mapY"":32.1}}]}}";

        var request = System.Text.Json.JsonSerializer.Deserialize<RaceContract.UpdateBoothCoordinatesRequest>(
            json,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        var item = Assert.Single(request.Coordinates);
        Assert.Equal(boothId, item.BoothId);
        Assert.Equal(65.4, item.MapX);
        Assert.Equal(32.1, item.MapY);
    }

    private sealed class TestRaceRepository(Race? race) : IRaceRepository
    {
        public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(race?.Id == raceId ? race : null);

        public Task CreateAsync(Race race, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(RacePageRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Race race, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateMapImageUrlAsync(Guid raceId, string mapImageUrl, string? modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<BoothListResultModel>> GetBoothListAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<ScoringLogResultModel> Items, int TotalItems)> GetScoringLogPageByRaceIdAsync(Guid raceId, Guid? teamId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int CompletedRegularBooths, int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetRaceTeamScoreAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateRaceTeamScoreAsync(Guid raceId, Guid teamId, int totalScore, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateScoringLogAsync(ScoringLog log, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateRaceMessageAsync(RaceMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(Guid raceId, int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsTeamInRaceAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BoothProgressResultModel> GetBoothProgressAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OVCMOVE.Application.Features.Races.Common.RaceTeamScoreMutation?> TryDebitRaceTeamScoreAsync(Guid raceId, Guid teamId, int amount, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ScoringLog>> GetScoringLogsByEventIdAsync(Guid raceId, string eventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<FinalizedBoothOutcome>> GetFinalizedBoothOutcomesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestBoothRepository(IEnumerable<Booth> booths) : IBoothRepository
    {
        private readonly List<Booth> _booths = booths.ToList();
        public List<BoothCoordinateItemModel> UpdatedBatches { get; } = [];
        public bool ThrowOnUpdate { get; set; }

        public Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Booth>>(_booths.Where(b => b.RaceId == raceId).ToArray());

        public Task UpdateCoordinatesBatchAsync(
            Guid raceId,
            IReadOnlyCollection<BoothCoordinateItemModel> coordinates,
            DateTime modifiedAt,
            string? modifiedBy,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnUpdate)
            {
                throw new InvalidOperationException("Simulated database failure.");
            }

            foreach (var item in coordinates)
            {
                var booth = _booths.FirstOrDefault(b => b.Id == item.BoothId && b.RaceId == raceId);
                if (booth != null)
                {
                    booth.MapX = item.MapX;
                    booth.MapY = item.MapY;
                    booth.ModifiedAt = modifiedAt;
                    booth.ModifiedBy = modifiedBy;
                }
                UpdatedBatches.Add(item);
            }

            return Task.CompletedTask;
        }

        public Task<Guid> CreateAsync(Booth booth, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Booth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Booth?> GetActiveByTeamAndRaceAsync(Guid teamId, Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Booth booth, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryRequestEntryAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryOccupyAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryRejectEntryAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryReleaseAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> SubmitScoreAndReleaseAsync(SubmitBoothScoreModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public bool HasActiveTransaction { get; private set; }
        public bool BeginCalled { get; private set; }
        public bool CommitCalled { get; private set; }
        public bool RollbackCalled { get; private set; }

        public Task BeginAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            BeginCalled = true;
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = false;
            CommitCalled = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = false;
            RollbackCalled = true;
            return Task.CompletedTask;
        }
    }
}
