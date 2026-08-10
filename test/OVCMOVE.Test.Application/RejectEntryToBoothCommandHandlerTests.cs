using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Booths.Commands.RejectEntryToBooth;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class RejectEntryToBoothCommandHandlerTests
{
    [Fact]
    public async Task Handle_AssignedPendingBooth_ReleasesAndNotifiesRejectedTeam()
    {
        var teamId = Guid.NewGuid();
        var booth = CreatePendingBooth(teamId);
        var notifications = new RecordingNotificationService();
        var handler = new RejectEntryToBoothCommandHandler(
            new StubBoothRepository(booth),
            new StubBoothOrganizerRepository(isAssigned: true),
            notifications);
        var command = new RejectEntryToBoothCommand(
            booth.Id,
            teamId,
            Guid.NewGuid());

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(
            (booth.RaceId, booth.Id, command.TeamId),
            notifications.RejectedEntry);
        Assert.Equal(BoothConstants.BoothStatus.Free, booth.Status);
        Assert.Null(booth.TeamId);
    }

    [Fact]
    public async Task Handle_UnassignedOrganizer_ThrowsForbidden()
    {
        var teamId = Guid.NewGuid();
        var booth = CreatePendingBooth(teamId);
        var notifications = new RecordingNotificationService();
        var handler = new RejectEntryToBoothCommandHandler(
            new StubBoothRepository(booth),
            new StubBoothOrganizerRepository(isAssigned: false),
            notifications);

        await Assert.ThrowsAsync<ApplicationForbiddenException>(() =>
            handler.Handle(
                new RejectEntryToBoothCommand(
                    booth.Id,
                    teamId,
                    Guid.NewGuid()),
                CancellationToken.None));

        Assert.Null(notifications.RejectedEntry);
    }

    [Fact]
    public async Task Handle_OccupiedBooth_ThrowsConflict()
    {
        var booth = CreateFreeBooth();
        booth.Status = BoothConstants.BoothStatus.Occupied;
        booth.TeamId = Guid.NewGuid();
        var notifications = new RecordingNotificationService();
        var handler = new RejectEntryToBoothCommandHandler(
            new StubBoothRepository(booth),
            new StubBoothOrganizerRepository(isAssigned: true),
            notifications);

        await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            handler.Handle(
                new RejectEntryToBoothCommand(
                    booth.Id,
                    Guid.NewGuid(),
                    Guid.NewGuid()),
                CancellationToken.None));

        Assert.Null(notifications.RejectedEntry);
    }

    private static Booth CreateFreeBooth() => new()
    {
        Id = Guid.NewGuid(),
        RaceId = Guid.NewGuid(),
        Status = BoothConstants.BoothStatus.Free
    };

    private static Booth CreatePendingBooth(Guid teamId) => new()
    {
        Id = Guid.NewGuid(),
        RaceId = Guid.NewGuid(),
        Status = BoothConstants.BoothStatus.Pending,
        TeamId = teamId
    };

    private sealed class StubBoothRepository(Booth? booth) : IBoothRepository
    {
        public Task<Booth?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(booth?.Id == id ? booth : null);

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

        public Task<bool> TryOccupyAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryRequestEntryAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TryRejectEntryAsync(
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default)
        {
            if (booth?.Id != boothId ||
                booth.Status != BoothConstants.BoothStatus.Pending ||
                booth.TeamId != teamId)
            {
                return Task.FromResult(false);
            }

            booth.Status = BoothConstants.BoothStatus.Free;
            booth.TeamId = null;
            return Task.FromResult(true);
        }

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

    private sealed class StubBoothOrganizerRepository(bool isAssigned)
        : IBoothOrganizerRepository
    {
        public Task<bool> IsAssignedAsync(
            Guid organizerId,
            Guid boothId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(isAssigned);

        public Task CreateAsync(
            BoothOrganizer boothOrganizer,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteByBoothIdAsync(
            Guid boothId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BoothOrganizer?> GetByOrganizerAndRaceAsync(
            Guid organizerId,
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<BoothOrganizer>> GetByRaceIdAsync(
            Guid raceId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingNotificationService
        : IBoothNotificationService
    {
        public (Guid RaceId, Guid BoothId, Guid TeamId)? RejectedEntry
        {
            get;
            private set;
        }

        public Task NotifyBoothEntryRejectedAsync(
            Guid raceId,
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default)
        {
            RejectedEntry = (raceId, boothId, teamId);
            return Task.CompletedTask;
        }

        public Task NotifyBoothStatusChangedAsync(
            Guid raceId,
            Guid boothId,
            string status,
            Guid? teamId,
            string? teamName,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceScoreChangedAsync(
            Guid raceId,
            Guid teamId,
            int delta,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task NotifyBoothEntryCancelledAsync(
            Guid raceId,
            Guid boothId,
            Guid teamId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
