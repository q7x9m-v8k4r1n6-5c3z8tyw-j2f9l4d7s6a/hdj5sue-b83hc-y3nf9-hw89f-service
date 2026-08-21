using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Booths.Commands.CancelBoothSession;
using OVCMOVE.Application.Features.Booths.Commands.RequestEntryToBooth;
using OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class BoothSessionCommandHandlerTests
{
    [Fact]
    public async Task RequestEntry_PersistsPendingSessionBeforeNotifying()
    {
        var team = new User
        {
            Id = Guid.NewGuid(),
            DisplayName = "Team A"
        };
        var booth = CreateBooth();
        var repository = new InMemoryBoothRepository(booth);
        var notifications = new BoothNotificationSpy();
        var handler = new RequestEntryToBoothCommandHandler(
            repository,
            new ValidBoothRaceRepository(),
            notifications,
            new StubTeamUserRepository(team));

        var result = await handler.Handle(
            new RequestEntryToBoothCommand
            {
                BoothId = booth.Id,
                TeamId = team.Id
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BoothConstants.BoothStatus.Pending, booth.Status);
        Assert.Equal(team.Id, booth.TeamId);
        Assert.Contains(
            notifications.StatusChanges,
            change => change.Status == BoothConstants.BoothStatus.Pending);
    }

    [Fact]
    public async Task CancelSession_DoubleClick_OnlyFirstRequestReleasesBooth()
    {
        var teamId = Guid.NewGuid();
        var booth = CreateBooth(teamId, BoothConstants.BoothStatus.Occupied);
        var repository = new InMemoryBoothRepository(booth);
        var notifications = new BoothNotificationSpy();
        var handler = new CancelBoothSessionCommandHandler(
            repository,
            new AssignedBoothOrganizerRepository(),
            notifications);
        var command = new CancelBoothSessionCommand(
            booth.Id,
            Guid.NewGuid());

        await handler.Handle(command, CancellationToken.None);
        await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(BoothConstants.BoothStatus.Free, booth.Status);
        Assert.Null(booth.TeamId);
        Assert.Equal(1, repository.CancelledSessionCount);
        Assert.Equal(1, notifications.CancelledCount);
    }

    [Fact]
    public async Task SubmitZeroScore_CompletesSessionAndNotifiesScoreChange()
    {
        var teamId = Guid.NewGuid();
        var booth = CreateBooth(teamId, BoothConstants.BoothStatus.Occupied);
        var repository = new InMemoryBoothRepository(booth);
        var notifications = new BoothNotificationSpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = CreateSubmitHandler(
            repository,
            notifications,
            unitOfWork);

        var result = await handler.Handle(
            CreateSubmitCommand(booth, teamId, score: 0),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(0, repository.LastSubmittedScore);
        Assert.Equal(1, repository.SubmittedScoreCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);
        Assert.Equal(BoothConstants.BoothStatus.Free, booth.Status);
        Assert.Null(booth.TeamId);
        Assert.Contains(
            notifications.ScoreChanges,
            change => change.TeamId == teamId && change.Delta == 0);
        Assert.Contains(
            notifications.CompletedBooths,
            completion => completion.BoothId == booth.Id &&
                completion.TeamId == teamId &&
                completion.BoothName == booth.Name &&
                completion.Score == 0);
    }

    [Fact]
    public async Task SubmitAndCancelConcurrently_OnlyOneTerminalActionSucceeds()
    {
        var teamId = Guid.NewGuid();
        var booth = CreateBooth(teamId, BoothConstants.BoothStatus.Occupied);
        var repository = new InMemoryBoothRepository(booth);
        var notifications = new BoothNotificationSpy();
        var submitHandler = CreateSubmitHandler(
            repository,
            notifications,
            new UnitOfWorkSpy());
        var cancelHandler = new CancelBoothSessionCommandHandler(
            repository,
            new AssignedBoothOrganizerRepository(),
            notifications);

        var submitTask = Record.ExceptionAsync(() =>
            submitHandler.Handle(
                CreateSubmitCommand(booth, teamId, score: 10),
                CancellationToken.None));
        var cancelTask = Record.ExceptionAsync(() =>
            cancelHandler.Handle(
                new CancelBoothSessionCommand(
                    booth.Id,
                    Guid.NewGuid()),
                CancellationToken.None));

        var exceptions = await Task.WhenAll(submitTask, cancelTask);

        Assert.Single(exceptions, exception => exception is not null);
        Assert.Contains(
            exceptions,
            exception => exception is ApplicationConflictException);
        Assert.Equal(
            1,
            repository.SubmittedScoreCount +
            repository.CancelledSessionCount);
        Assert.Equal(BoothConstants.BoothStatus.Free, booth.Status);
        Assert.Null(booth.TeamId);
    }

    private static SubmitBoothScoreCommandHandler CreateSubmitHandler(
        InMemoryBoothRepository repository,
        BoothNotificationSpy notifications,
        UnitOfWorkSpy unitOfWork) =>
        new(
            repository,
            notifications,
            unitOfWork,
            new ValidBoothRaceRepository(),
            new AssignedBoothOrganizerRepository());

    private static SubmitBoothScoreCommand CreateSubmitCommand(
        Booth booth,
        Guid teamId,
        int score) =>
        new()
        {
            BoothID = booth.Id,
            TeamID = teamId,
            OrganizerId = Guid.NewGuid(),
            Score = score
        };

    private static Booth CreateBooth(
        Guid? teamId = null,
        string? status = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            RaceId = Guid.NewGuid(),
            TeamId = teamId,
            Status = status ?? BoothConstants.BoothStatus.Free,
            Name = "Booth A"
        };
}
