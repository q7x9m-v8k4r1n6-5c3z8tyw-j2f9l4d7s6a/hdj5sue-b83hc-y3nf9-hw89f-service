using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Test.Application;

public sealed class SendRaceMessageCommandHandlerTests
{
    [Fact]
    public async Task Handle_rejects_mismatched_recipient_type_and_key()
    {
        var handler = new SendRaceMessageCommandHandler(
            new RaceRepositoryDouble(),
            new BoothNotificationServiceDouble(),
            new UnitOfWorkSpy());

        var command = new SendRaceMessageCommand
        {
            RaceId = Guid.NewGuid(),
            SenderId = Guid.NewGuid(),
            Body = "Thông báo",
            Recipients =
            [
                new RaceMessageRecipientModel
                {
                    Key = RaceMessageRecipientConstants.All,
                    Label = "Tất cả mọi người",
                    Type = RaceMessageRecipientConstants.Team
                }
            ]
        };

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_normalizes_valid_scoped_recipient_before_saving()
    {
        var repository = new RaceRepositoryDouble();
        var notification = new BoothNotificationServiceDouble();
        var handler = new SendRaceMessageCommandHandler(
            repository,
            notification,
            new UnitOfWorkSpy());
        var teamId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        repository.RaceDetail = new RaceDetailResultModel
        {
            Id = raceId,
            RaceTeam =
            [
                new RaceTeamModel
                {
                    TeamId = teamId,
                    Name = "Team A"
                }
            ]
        };

        var result = await handler.Handle(
            new SendRaceMessageCommand
            {
                RaceId = raceId,
                SenderId = Guid.NewGuid(),
                Body = "  Thông báo mới  ",
                Recipients =
                [
                    new RaceMessageRecipientModel
                    {
                        Key = $"TEAM:{teamId:D}",
                        Label = "Team A",
                        Type = "TEAM"
                    }
                ]
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(repository.CreatedMessage);
        Assert.Equal("Thông báo mới", repository.CreatedMessage.Body);
        Assert.Contains($"team:{teamId:D}", repository.CreatedMessage.RecipientKeysJson);
        Assert.Equal(raceId, notification.RaceId);
        Assert.NotNull(notification.Message);
    }

    [Fact]
    public async Task Handle_joins_outer_transaction_and_defers_realtime_notification()
    {
        var repository = new RaceRepositoryDouble();
        var notification = new BoothNotificationServiceDouble();
        var unitOfWork = new UnitOfWorkSpy();
        await unitOfWork.BeginAsync();
        var handler = new SendRaceMessageCommandHandler(
            repository,
            notification,
            unitOfWork);
        var raceId = Guid.NewGuid();

        var result = await handler.Handle(
            new SendRaceMessageCommand
            {
                RaceId = raceId,
                SenderId = Guid.NewGuid(),
                Body = "Thông báo workflow",
                PublishRealtimeNotification = false,
                Recipients =
                [
                    new RaceMessageRecipientModel
                    {
                        Key = RaceMessageRecipientConstants.All,
                        Label = "Tất cả mọi người",
                        Type = RaceMessageRecipientConstants.All
                    }
                ]
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(repository.CreatedMessage);
        Assert.True(unitOfWork.HasActiveTransaction);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(0, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);
        Assert.Null(notification.Message);
    }

    [Fact]
    public async Task Update_score_joins_outer_transaction_and_defers_realtime_notification()
    {
        var repository = new RaceRepositoryDouble { CurrentScore = 20 };
        var notification = new BoothNotificationServiceDouble();
        var unitOfWork = new UnitOfWorkSpy();
        await unitOfWork.BeginAsync();
        var handler = new UpdateTeamScoreCommandHandler(
            repository,
            notification,
            unitOfWork);

        var result = await handler.Handle(
            new UpdateTeamScoreCommand
            {
                RaceId = Guid.NewGuid(),
                TeamId = Guid.NewGuid(),
                Delta = 15,
                Reason = "Workflow Engineer Card",
                PublishRealtimeNotification = false
            },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(35, repository.CurrentScore);
        Assert.NotNull(repository.CreatedScoringLog);
        Assert.True(unitOfWork.HasActiveTransaction);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(0, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);
        Assert.Equal(0, notification.ScoreNotificationCount);
    }

    private sealed class BoothNotificationServiceDouble : IBoothNotificationService
    {
        public Guid RaceId { get; private set; }
        public RaceMessageResultModel? Message { get; private set; }
        public int ScoreNotificationCount { get; private set; }

        public Task NotifyBoothStatusChangedAsync(Guid raceId, Guid boothId, string status, Guid? teamId, string? teamName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceScoreChangedAsync(Guid raceId, Guid teamId, int delta, CancellationToken cancellationToken = default)
        {
            ScoreNotificationCount++;
            return Task.CompletedTask;
        }

        public Task NotifyBoothEntryCancelledAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyBoothEntryRejectedAsync(Guid raceId, Guid boothId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceMessageAsync(Guid raceId, RaceMessageResultModel message, CancellationToken cancellationToken = default)
        {
            RaceId = raceId;
            Message = message;
            return Task.CompletedTask;
        }
    }

    private sealed class RaceRepositoryDouble : IRaceRepository
    {
        public RaceMessage? CreatedMessage { get; private set; }
        public RaceDetailResultModel? RaceDetail { get; set; }
        public int? CurrentScore { get; set; }
        public ScoringLog? CreatedScoringLog { get; private set; }

        public Task CreateRaceMessageAsync(RaceMessage message, CancellationToken cancellationToken = default)
        {
            CreatedMessage = message;
            return Task.CompletedTask;
        }

        public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Race?>(new Race { Id = raceId });

        public Task CreateAsync(Race race, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(
            RacePageRequestModel request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<RaceDetailResultModel?>(RaceDetail ?? new RaceDetailResultModel { Id = raceId });

        public Task<bool> UpdateAsync(Race race, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(Guid? raceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<BoothListResultModel>> GetBoothListAsync(Guid? raceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyCollection<ScoringLogResultModel> Items, int TotalItems)> GetScoringLogPageByRaceIdAsync(Guid raceId, Guid? teamId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(int CompletedRegularBooths, int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int?> GetRaceTeamScoreAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentScore);

        public Task<bool> UpdateRaceTeamScoreAsync(Guid raceId, Guid teamId, int totalScore, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) =>
            Task.FromResult(SetCurrentScore(totalScore));

        public Task CreateScoringLogAsync(ScoringLog log, CancellationToken cancellationToken = default)
        {
            CreatedScoringLog = log;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(Guid raceId, int limit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> IsTeamInRaceAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BoothProgressResultModel> GetBoothProgressAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        private bool SetCurrentScore(int totalScore)
        {
            CurrentScore = totalScore;
            return true;
        }
    }
}
