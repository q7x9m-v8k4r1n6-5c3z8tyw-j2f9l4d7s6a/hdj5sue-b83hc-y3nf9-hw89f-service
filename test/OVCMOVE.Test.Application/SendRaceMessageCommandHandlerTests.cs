using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Races.Command.SendRaceMessage;
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
            new BoothNotificationServiceDouble());

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
        var handler = new SendRaceMessageCommandHandler(repository, notification);
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

    private sealed class BoothNotificationServiceDouble : IBoothNotificationService
    {
        public Guid RaceId { get; private set; }
        public RaceMessageResultModel? Message { get; private set; }

        public Task NotifyBoothStatusChangedAsync(Guid raceId, Guid boothId, string status, Guid? teamId, string? teamName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyRaceScoreChangedAsync(Guid raceId, Guid teamId, int delta, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

        public Task CreateRaceMessageAsync(RaceMessage message, CancellationToken cancellationToken = default)
        {
            CreatedMessage = message;
            return Task.CompletedTask;
        }

        public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Race?>(new Race { Id = raceId });

        public Task CreateAsync(Race race, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(int page, int pageSize, Guid? teamId, CancellationToken cancellationToken = default) =>
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
            throw new NotSupportedException();

        public Task<bool> UpdateRaceTeamScoreAsync(Guid raceId, Guid teamId, int totalScore, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task CreateScoringLogAsync(ScoringLog log, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(Guid raceId, int limit, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> IsTeamInRaceAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BoothProgressResultModel> GetBoothProgressAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
