using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.Race;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Application.Features.Races.Common;
using OVCMOVE.Application.Features.Races.Query.BoothList;
using OVCMOVE.Application.Features.Races.Query.ScoringLog;
using OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;
using OVCMOVE.Domain.Entities;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class OverclockServiceTests
{
    [Fact]
    public async Task Resolve_TreatsMissingFailedOutcomeAsIncorrectPrediction()
    {
        var raceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var failedTeamId = Guid.NewGuid();
        var succeededTeamId = Guid.NewGuid();
        var unknownTeamId = Guid.NewGuid();
        var failedBoothId = Guid.NewGuid();
        var succeededBoothId = Guid.NewGuid();
        var unknownBoothId = Guid.NewGuid();
        var effect = new CardEffectDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            RaceId = raceId.ToString(),
            CardId = CardIds.Overclock,
            CardInstanceId = Guid.NewGuid().ToString(),
            CardUseId = Guid.NewGuid().ToString(),
            OwnerTeamId = ownerId.ToString(),
            TriggerEventCode = CardEffectEventCodes.OverclockResolution,
            Status = CardEffectStatus.Active,
            Data = new BsonDocument
            {
                ["cdSteal"] = 15,
                ["cdSelfPenalty"] = 5,
                ["predictions"] = new BsonArray
                {
                    Prediction(failedTeamId, failedBoothId),
                    Prediction(succeededTeamId, succeededBoothId),
                    Prediction(unknownTeamId, unknownBoothId)
                }
            }
        };
        var cardRepository = new OverclockCardRepository(raceId, effect);
        var raceRepository = new OverclockRaceRepository(
        [
            new(failedTeamId, failedBoothId, 0, DateTime.UtcNow),
            new(succeededTeamId, succeededBoothId, 20, DateTime.UtcNow)
        ]);
        var sender = new ScoreSender();
        var service = new OverclockService(
            cardRepository,
            raceRepository,
            new UnitOfWorkSpy(),
            sender,
            new BoothNotificationSpy(),
            NullLogger<OverclockService>.Instance);

        var response = await service.ResolveAsync(raceId, Guid.NewGuid());

        Assert.Equal(OverclockWindowStatus.Resolved, response.Status);
        Assert.Equal(3, response.PredictionCount);
        Assert.Equal(1, response.CorrectCount);
        Assert.Equal(2, response.IncorrectCount);
        Assert.Equal(0, response.NotEvaluatedCount);
        Assert.Contains(sender.Commands, item => item.TeamId == ownerId && item.Delta == 5);
        Assert.Contains(sender.Commands, item => item.TeamId == failedTeamId && item.Delta == -15);
        Assert.DoesNotContain(sender.Commands, item => item.TeamId == unknownTeamId);
        Assert.Equal(CardEffectStatus.Resolved, effect.Status);
        Assert.Equal(OverclockWindowStatus.Resolved, cardRepository.Document.OverclockWindow.Status);
    }

    private static BsonDocument Prediction(Guid teamId, Guid boothId) => new()
    {
        ["targetTeamId"] = teamId.ToString(),
        ["boothId"] = boothId.ToString()
    };

    private sealed class OverclockCardRepository : IRaceCardRepository
    {
        private readonly CardEffectDocument _effect;

        public OverclockCardRepository(Guid raceId, CardEffectDocument effect)
        {
            _effect = effect;
            Document = new RaceCardDocument
            {
                Id = raceId.ToString(),
                RaceId = raceId.ToString(),
                OverclockWindow = new OverclockWindowState
                {
                    Status = OverclockWindowStatus.Open
                }
            };
        }

        public RaceCardDocument Document { get; }
        public Task<RaceCardDocument> GetOrCreateAsync(Guid raceId, CancellationToken cancellationToken = default) => Task.FromResult(Document);
        public Task ReplaceAsync(RaceCardDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveEffectsByCardAsync(Guid raceId, string cardId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CardEffectDocument>>(
                _effect.Status == CardEffectStatus.Active ? [_effect] : []);
        public Task ClaimEffectsAsync(Guid raceId, IReadOnlyCollection<string> effectIds, string eventId, DateTime claimedAt, CancellationToken cancellationToken = default)
        {
            _effect.ClaimedByEventId = eventId;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyCollection<CardEffectDocument>> GetClaimedEffectsAsync(Guid raceId, string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CardEffectDocument>>(
                _effect.ClaimedByEventId == eventId ? [_effect] : []);
        public Task CompleteClaimedEffectsAsync(Guid raceId, string eventCode, string eventId, Guid triggeredByTeamId, DateTime resolvedAt, IReadOnlyCollection<CardEffectResolution> resolutions, CancellationToken cancellationToken = default)
        {
            _effect.Status = CardEffectStatus.Resolved;
            _effect.ResolvedByEventId = eventId;
            return Task.CompletedTask;
        }
        public Task ReleaseClaimedEffectsAsync(Guid raceId, string eventId, DateTime releasedAt, CancellationToken cancellationToken = default)
        {
            _effect.ClaimedByEventId = null;
            return Task.CompletedTask;
        }

        public Task EnsureIndexesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReplaceWithEffectAsync(RaceCardDocument document, CardEffectDocument effect, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasActiveTrapAsync(Guid raceId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> TryClaimTrapAsync(Guid raceId, Guid boothId, Guid triggeringTeamId, DateTime triggeredAt, string resolvedByEventCode, string resolvedByEventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPendingReviveAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> ResolveReviveAsync(Guid raceId, string effectId, Guid organizerId, string resolution, DateTime confirmedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> GetEffectAsync(Guid raceId, string effectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveBoothResultEffectsAsync(Guid raceId, Guid teamId, DateTime occurredAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class OverclockRaceRepository(
        IReadOnlyCollection<FinalizedBoothOutcome> outcomes) : IRaceRepository
    {
        public Task<IReadOnlyCollection<FinalizedBoothOutcome>> GetFinalizedBoothOutcomesAsync(Guid raceId, CancellationToken cancellationToken = default) => Task.FromResult(outcomes);
        public Task CreateAsync(Race race, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<RaceItemResultModel> Items, int TotalItems)> GetPageAsync(RacePageRequestModel request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(Race race, DateTime expectedModifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<TeamLeaderboardResultModel>> GetLeaderboardAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<BoothListResultModel>> GetBoothListAsync(Guid? raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyCollection<ScoringLogResultModel> Items, int TotalItems)> GetScoringLogPageByRaceIdAsync(Guid raceId, Guid? teamId, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int CompletedRegularBooths, int CompletedHiddenBooths)> GetCompletedBoothStatsAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int?> GetRaceTeamScoreAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateRaceTeamScoreAsync(Guid raceId, Guid teamId, int totalScore, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<OVCMOVE.Application.Features.Races.Common.RaceTeamScoreMutation?> TryDebitRaceTeamScoreAsync(Guid raceId, Guid teamId, int amount, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateScoringLogAsync(ScoringLog log, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<ScoringLog>> GetScoringLogsByEventIdAsync(Guid raceId, string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ScoringLog>>([]);
        public Task CreateRaceMessageAsync(RaceMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<RaceMessageResultModel>> GetRaceMessagesAsync(Guid raceId, int limit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsTeamInRaceAsync(Guid raceId, Guid teamId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string?> GetRulesAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BoothProgressResultModel> GetBoothProgressAsync(Guid raceId, Guid teamId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ScoreSender : ISender
    {
        public List<UpdateTeamScoreCommand> Commands { get; } = [];
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var command = Assert.IsType<UpdateTeamScoreCommand>(request);
            Commands.Add(command);
            object result = new UpdateTeamScoreResult(command.RaceId, command.TeamId, 0, command.Delta, command.Delta);
            return Task.FromResult((TResponse)result);
        }
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, [EnumeratorCancellation] CancellationToken cancellationToken = default) { await Task.CompletedTask; yield break; }
        public async IAsyncEnumerable<object?> CreateStream(object request, [EnumeratorCancellation] CancellationToken cancellationToken = default) { await Task.CompletedTask; yield break; }
    }
}
