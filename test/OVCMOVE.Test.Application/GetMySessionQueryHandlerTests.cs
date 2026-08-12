using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Teams.Query.GetMySession;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class GetMySessionQueryHandlerTests
{
    [Theory]
    [InlineData(BoothConstants.BoothStatus.Pending)]
    [InlineData(BoothConstants.BoothStatus.Occupied)]
    public async Task Handle_ActiveSession_MapsPersistedBoothState(
        string status)
    {
        var teamId = Guid.NewGuid();
        var booth = new Booth
        {
            Id = Guid.NewGuid(),
            RaceId = Guid.NewGuid(),
            TeamId = teamId,
            Name = "Booth A",
            Place = "Campus",
            Description = "Session booth",
            IsHidden = true,
            Status = status
        };
        var handler = new GetMySessionQueryHandler(
            new StubBoothRepository(booth));

        var result = await handler.Handle(
            new GetMySessionQuery(booth.RaceId, teamId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(booth.RaceId, result.RaceId);
        Assert.Equal(booth.Id, result.BoothId);
        Assert.Equal(booth.Name, result.BoothName);
        Assert.Equal(booth.Status, result.Status);
        Assert.True(result.IsHidden);
    }

    [Fact]
    public async Task Handle_NoActiveSession_ReturnsNull()
    {
        var handler = new GetMySessionQueryHandler(
            new StubBoothRepository(null));

        var result = await handler.Handle(
            new GetMySessionQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubBoothRepository(Booth? booth)
        : IBoothRepository
    {
        public Task<Booth?> GetActiveByTeamAndRaceAsync(
            Guid teamId,
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                booth?.TeamId == teamId && booth.RaceId == raceId
                    ? booth
                    : null);

        public Task<Booth?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Guid> CreateAsync(
            Booth value,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            Booth value,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryRequestEntryAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryOccupyAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryRejectEntryAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryReleaseAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            Guid boothId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SubmitScoreAndReleaseAsync(
            SubmitBoothScoreModel model,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
