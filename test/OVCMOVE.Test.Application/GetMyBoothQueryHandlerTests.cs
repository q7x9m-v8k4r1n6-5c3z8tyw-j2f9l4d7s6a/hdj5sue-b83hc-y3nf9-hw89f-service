using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Application.Features.Booths.Query.GetMyBooth;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class GetMyBoothQueryHandlerTests
{
    [Theory]
    [InlineData(BoothConstants.BoothStatus.Pending)]
    [InlineData(BoothConstants.BoothStatus.Occupied)]
    public async Task Handle_AssignedBooth_RestoresPersistedSession(
        string status)
    {
        var organizerId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var booth = new Booth
        {
            Id = Guid.NewGuid(),
            RaceId = Guid.NewGuid(),
            TeamId = teamId,
            Status = status,
            Name = "Booth A"
        };
        var assignment = new BoothOrganizer
        {
            Id = Guid.NewGuid(),
            RaceId = booth.RaceId,
            BoothId = booth.Id,
            OrganizerId = organizerId
        };
        var team = new User
        {
            Id = teamId,
            DisplayName = "Team A"
        };
        var handler = new GetMyBoothQueryHandler(
            new StubBoothOrganizerRepository(assignment),
            new StubBoothRepository(booth),
            new StubUserRepository(team));

        var result = await handler.Handle(
            new GetMyBoothQuery
            {
                RaceId = booth.RaceId,
                OrganizerId = organizerId
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
        Assert.Equal(teamId, result.TeamId);
        Assert.Equal(team.DisplayName, result.TeamName);
    }

    private sealed class StubBoothRepository(Booth booth) : IBoothRepository
    {
        public Task<Booth?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Booth?>(booth.Id == id ? booth : null);

        public Task<Booth?> GetActiveByTeamAndRaceAsync(
            Guid teamId,
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Guid> CreateAsync(Booth value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Booth>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Booth value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryRequestEntryAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryOccupyAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryRejectEntryAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> TryReleaseAsync(Guid boothId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> SubmitScoreAndReleaseAsync(SubmitBoothScoreModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubBoothOrganizerRepository(
        BoothOrganizer assignment) : IBoothOrganizerRepository
    {
        public Task<BoothOrganizer?> GetByOrganizerAndRaceAsync(
            Guid organizerId,
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BoothOrganizer?>(
                assignment.OrganizerId == organizerId &&
                assignment.RaceId == raceId
                    ? assignment
                    : null);

        public Task CreateAsync(BoothOrganizer value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteByBoothIdAsync(Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<BoothOrganizer>> GetByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsAssignedAsync(Guid organizerId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByUsernameAnyStatusAsync(string username, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailAnyStatusAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(User value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateDisplayNameAsync(Guid id, string displayName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateGoogleProfileAsync(Guid id, string? displayName, string? avatarUrl, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> SoftDeleteAsync(Guid id, string userType, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
