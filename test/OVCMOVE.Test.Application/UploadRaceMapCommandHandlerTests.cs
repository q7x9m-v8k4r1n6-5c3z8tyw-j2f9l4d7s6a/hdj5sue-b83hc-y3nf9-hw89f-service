using System.Text;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Races.Command.UploadRaceMap;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public class UploadRaceMapCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_UploadsMapUpdatesDatabaseAndCleansUpPreviousBlob()
    {
        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Status = RaceConstants.RaceStatus.Ongoing,
            MapImageUrl = "https://storage.blob.core.windows.net/race-map/old-map.png"
        };
        var repository = new TestRaceRepository(race);
        var blobStorage = new TestBlobStorageService("https://storage.blob.core.windows.net/race-map/new-map.png");
        var handler = new UploadRaceMapCommandHandler(repository, blobStorage);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-map-bytes"));
        var command = new UploadRaceMapCommand
        {
            RaceId = raceId,
            File = new FileUploadModel(stream, "new-map.png", "image/png")
        };

        var resultUrl = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("https://storage.blob.core.windows.net/race-map/new-map.png", resultUrl);
        Assert.Equal(blobStorage.MapContainerName, blobStorage.LastUploadedContainer);
        Assert.Equal("new-map.png", blobStorage.LastUploadedFileName);
        Assert.Equal(raceId, repository.LastUpdatedRaceId);
        Assert.Equal(resultUrl, repository.LastUpdatedMapImageUrl);
        Assert.Contains(race.MapImageUrl, blobStorage.DeletedUrls);
    }

    [Fact]
    public async Task Handle_EmptyRaceId_ThrowsValidationException()
    {
        var handler = new UploadRaceMapCommandHandler(new TestRaceRepository(null), new TestBlobStorageService());
        await using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadRaceMapCommand
        {
            RaceId = Guid.Empty,
            File = new FileUploadModel(stream, "map.png", "image/png")
        };

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NullFile_ThrowsValidationException()
    {
        var handler = new UploadRaceMapCommandHandler(new TestRaceRepository(null), new TestBlobStorageService());
        var command = new UploadRaceMapCommand
        {
            RaceId = Guid.NewGuid(),
            File = null!
        };

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RaceNotFound_ThrowsNotFoundException()
    {
        var handler = new UploadRaceMapCommandHandler(new TestRaceRepository(null), new TestBlobStorageService());
        await using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadRaceMapCommand
        {
            RaceId = Guid.NewGuid(),
            File = new FileUploadModel(stream, "map.png", "image/png")
        };

        await Assert.ThrowsAsync<ApplicationNotFoundException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RaceCompleted_ThrowsConflictException()
    {
        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Status = RaceConstants.RaceStatus.Completed
        };
        var handler = new UploadRaceMapCommandHandler(new TestRaceRepository(race), new TestBlobStorageService());
        await using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadRaceMapCommand
        {
            RaceId = raceId,
            File = new FileUploadModel(stream, "map.png", "image/png")
        };

        await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DatabaseUpdateFails_CompensatesByDeletingUploadedBlob()
    {
        var raceId = Guid.NewGuid();
        var race = new Race
        {
            Id = raceId,
            Status = RaceConstants.RaceStatus.Ongoing
        };
        var repository = new TestRaceRepository(race) { ShouldUpdateFail = true };
        var blobStorage = new TestBlobStorageService("https://storage.blob.core.windows.net/race-map/uploaded-map.png");
        var handler = new UploadRaceMapCommandHandler(repository, blobStorage);

        await using var stream = new MemoryStream([1, 2, 3]);
        var command = new UploadRaceMapCommand
        {
            RaceId = raceId,
            File = new FileUploadModel(stream, "map.png", "image/png")
        };

        await Assert.ThrowsAsync<ApplicationConflictException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Contains("https://storage.blob.core.windows.net/race-map/uploaded-map.png", blobStorage.DeletedUrls);
    }

    private sealed class TestBlobStorageService(string defaultUploadUrl = "https://storage.blob.core.windows.net/race-map/default.png") : IBlobStorageService
    {
        public string MapContainerName => "race-map";
        public string? LastUploadedContainer { get; private set; }
        public string? LastUploadedFileName { get; private set; }
        public List<string> DeletedUrls { get; } = [];

        public Task<string> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string? containerName = null,
            CancellationToken cancellationToken = default)
        {
            LastUploadedContainer = containerName;
            LastUploadedFileName = fileName;
            return Task.FromResult(defaultUploadUrl);
        }

        public Task<bool> TryDeleteAsync(
            string fileUrl,
            string? containerName = null,
            CancellationToken cancellationToken = default)
        {
            DeletedUrls.Add(fileUrl);
            return Task.FromResult(true);
        }
    }

    private sealed class TestRaceRepository(Race? race) : IRaceRepository
    {
        public bool ShouldUpdateFail { get; set; }
        public Guid? LastUpdatedRaceId { get; private set; }
        public string? LastUpdatedMapImageUrl { get; private set; }

        public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(race?.Id == raceId ? race : null);

        public Task<bool> UpdateMapImageUrlAsync(
            Guid raceId,
            string mapImageUrl,
            string? modifiedBy,
            DateTime modifiedAt,
            CancellationToken cancellationToken = default)
        {
            if (ShouldUpdateFail)
            {
                return Task.FromResult(false);
            }

            LastUpdatedRaceId = raceId;
            LastUpdatedMapImageUrl = mapImageUrl;
            return Task.FromResult(true);
        }

        public Task CreateAsync(Race race, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(RacePageRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Race race, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
    }
}
