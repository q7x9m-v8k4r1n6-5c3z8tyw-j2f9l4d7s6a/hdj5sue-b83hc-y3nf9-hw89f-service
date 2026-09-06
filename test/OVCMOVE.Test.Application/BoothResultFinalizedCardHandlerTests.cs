using System.Runtime.CompilerServices;
using MediatR;
using MongoDB.Bson;
using OVCMOVE.Application.Abstractions.Plugins;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE.Domain.Constants;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;
using OVCMOVE2026.Plugin.Services;

namespace OVCMOVE.Test.Application;

public sealed class BoothResultFinalizedCardHandlerTests
{
    [Fact]
    public async Task Engineer_bonus_is_included_in_Cupid_reward()
    {
        var raceId = Guid.NewGuid();
        var targetTeamId = Guid.NewGuid();
        var cupidOwnerId = Guid.NewGuid();
        var repository = new FinalizedEffectRepository
        {
            Effects =
            [
                Effect(CardIds.Engineer, targetTeamId, new BsonDocument
                {
                    ["requiredBoothType"] = BoothConstants.BoothType.Intellectual,
                    ["scoreMultiplier"] = 2.0,
                    ["qualificationMode"] = "any_success"
                }),
                Effect(CardIds.Cupid, cupidOwnerId, new BsonDocument
                {
                    ["rewardMultiplier"] = 1.0,
                    ["failurePenalty"] = 5,
                    ["timeBetweenUseMinutes"] = 15
                }, targetTeamId)
            ]
        };
        var sender = new ScoreCommandSender();
        var handler = new BoothResultFinalizedCardHandler(repository, sender);
        var payload = new BoothResultFinalizedData
        {
            BoothCompletionId = Guid.NewGuid(),
            BoothType = BoothConstants.BoothType.Intellectual,
            SubmittedPoints = 20,
            FinalAwardedPoints = 20,
            Result = BoothResultValues.Succeeded
        };

        await handler.HandleAsync(
            new PluginEventContext(
                PluginEventNames.BoothResultFinalized,
                raceId,
                targetTeamId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                "booth-result:test",
                payload),
            CancellationToken.None);

        Assert.Equal(40, payload.FinalAwardedPoints);
        Assert.Collection(
            sender.Commands,
            command =>
            {
                Assert.Equal(targetTeamId, command.TeamId);
                Assert.Equal(20, command.Delta);
            },
            command =>
            {
                Assert.Equal(cupidOwnerId, command.TeamId);
                Assert.Equal(40, command.Delta);
            });
        Assert.Equal(2, repository.Resolutions.Count);
        var cupidResolution = Assert.Single(
            repository.Resolutions,
            item => item.Result.TryGetValue("ownerDelta", out _));
        Assert.Equal(40, cupidResolution.Result["finalAwardedPoints"].AsInt32);
        Assert.Equal(40, cupidResolution.Result["ownerDelta"].AsInt32);
    }

    private static CardEffectDocument Effect(
        string cardId,
        Guid ownerTeamId,
        BsonDocument data,
        Guid? targetTeamId = null) => new()
        {
            Id = ObjectId.GenerateNewId().ToString(),
            RaceId = Guid.NewGuid().ToString(),
            CardId = cardId,
            CardInstanceId = Guid.NewGuid().ToString(),
            CardUseId = Guid.NewGuid().ToString(),
            OwnerTeamId = ownerTeamId.ToString(),
            TargetTeamId = (targetTeamId ?? ownerTeamId).ToString(),
            TriggerEventCode = CardEffectEventCodes.BoothResultFinalized,
            Status = CardEffectStatus.Active,
            StartAt = DateTime.UtcNow,
            Data = data
        };

    private sealed class FinalizedEffectRepository : IRaceCardRepository
    {
        public IReadOnlyCollection<CardEffectDocument> Effects { get; init; } = [];
        public IReadOnlyCollection<CardEffectResolution> Resolutions { get; private set; } = [];

        public Task<IReadOnlyCollection<CardEffectDocument>> GetActiveBoothResultEffectsAsync(
            Guid raceId,
            Guid teamId,
            DateTime occurredAt,
            CancellationToken cancellationToken = default) => Task.FromResult(Effects);

        public Task ResolveEffectsAsync(
            Guid raceId,
            string eventCode,
            string eventId,
            Guid triggeredByTeamId,
            DateTime resolvedAt,
            IReadOnlyCollection<CardEffectResolution> resolutions,
            CancellationToken cancellationToken = default)
        {
            Resolutions = resolutions;
            return Task.CompletedTask;
        }

        public Task EnsureIndexesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RaceCardDocument> GetOrCreateAsync(Guid raceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReplaceAsync(RaceCardDocument document, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReplaceWithEffectAsync(RaceCardDocument document, CardEffectDocument effect, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasActiveTrapAsync(Guid raceId, Guid boothId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> TryClaimTrapAsync(Guid raceId, Guid boothId, Guid triggeringTeamId, DateTime triggeredAt, string resolvedByEventCode, string resolvedByEventId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> ConfirmReviveAsync(Guid raceId, string effectId, Guid organizerId, DateTime confirmedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CardEffectDocument?> GetEffectAsync(Guid raceId, string effectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ScoreCommandSender : ISender
    {
        public List<UpdateTeamScoreCommand> Commands { get; } = [];

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            var command = Assert.IsType<UpdateTeamScoreCommand>(request);
            Commands.Add(command);
            object response = new UpdateTeamScoreResult(
                command.RaceId,
                command.TeamId,
                0,
                command.Delta,
                command.Delta);
            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(
            object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<object?> CreateStream(
            object request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
