using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;
using OVCMOVE2026.Plugin.Models;
using OVCMOVE2026.Plugin.Repositories;

namespace OVCMOVE2026.Plugin.Services;

public interface IOverclockService
{
    Task<OverclockWindowResponse> GetAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<OverclockWindowResponse> OpenAsync(Guid raceId, Guid actorId, CancellationToken cancellationToken = default);
    Task<OverclockResolutionResponse> ResolveAsync(Guid raceId, Guid actorId, CancellationToken cancellationToken = default);
}

public sealed class OverclockService(
    IRaceCardRepository cardRepository,
    IRaceRepository raceRepository,
    IUnitOfWork unitOfWork,
    ISender sender,
    IBoothNotificationService notificationService,
    ILogger<OverclockService> logger) : IOverclockService
{
    public async Task<OverclockWindowResponse> GetAsync(
        Guid raceId,
        CancellationToken cancellationToken = default) =>
        ToResponse((await cardRepository.GetOrCreateAsync(raceId, cancellationToken)).OverclockWindow);

    public async Task<OverclockWindowResponse> OpenAsync(
        Guid raceId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var document = await cardRepository.GetOrCreateAsync(raceId, cancellationToken);
        if (document.OverclockWindow.Status == OverclockWindowStatus.Open)
            return ToResponse(document.OverclockWindow);
        if (document.OverclockWindow.Status != OverclockWindowStatus.NotOpened)
            throw new ApplicationConflictException(
                "Overclock đã đóng hoặc đã được chốt và không thể mở lại.");

        document.OverclockWindow = new OverclockWindowState
        {
            Status = OverclockWindowStatus.Open,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = actorId.ToString()
        };
        await cardRepository.ReplaceAsync(document, cancellationToken);
        return ToResponse(document.OverclockWindow);
    }

    public async Task<OverclockResolutionResponse> ResolveAsync(
        Guid raceId,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var document = await cardRepository.GetOrCreateAsync(raceId, cancellationToken);
        if (document.OverclockWindow.Status == OverclockWindowStatus.Resolved)
            return new(OverclockWindowStatus.Resolved, 0, 0, 0, 0);
        if (document.OverclockWindow.Status != OverclockWindowStatus.Open)
            throw new ApplicationConflictException("Màn dự đoán Overclock chưa được mở.");

        var now = DateTime.UtcNow;
        var eventId = $"overclock-resolution:{raceId:N}:{Guid.NewGuid():N}";
        document.OverclockWindow.Status = OverclockWindowStatus.Closed;
        document.OverclockWindow.ClosedAt = now;
        document.OverclockWindow.ClosedBy = actorId.ToString();
        document.OverclockWindow.ResolutionEventId = eventId;
        await cardRepository.ReplaceAsync(document, cancellationToken);

        var effects = await cardRepository.GetActiveEffectsByCardAsync(
            raceId, CardIds.Overclock, cancellationToken);
        if (effects.Count == 0)
        {
            if (!await TryMarkResolvedAsync(raceId, now, cancellationToken))
                throw new ApplicationConflictException(
                    "Không thể lưu trạng thái Overclock đã chốt. Vui lòng thử lại.");
            return new(OverclockWindowStatus.Resolved, 0, 0, 0, 0);
        }

        var commitStarted = false;
        var scoreAdjustments = new Dictionary<Guid, int>();
        var resolutions = new List<CardEffectResolution>();
        var predictionCount = 0;
        var correctCount = 0;
        var incorrectCount = 0;
        var notEvaluatedCount = 0;
        try
        {
            await cardRepository.ClaimEffectsAsync(
                raceId,
                effects.Select(item => item.Id).ToArray(),
                eventId,
                now,
                cancellationToken);

            var outcomes = await raceRepository.GetFinalizedBoothOutcomesAsync(
                raceId, cancellationToken);
            var outcomeLookup = outcomes
                .GroupBy(item => (item.TeamId, item.BoothId))
                .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.FinalizedAt).First());

            foreach (var effect in effects)
            {
                if (!Guid.TryParse(effect.OwnerTeamId, out var ownerTeamId))
                    throw new ApplicationValidationException("Overclock effect có ownerTeamId không hợp lệ.");
                if (!effect.Data.TryGetValue("predictions", out var rawPredictions) || !rawPredictions.IsBsonArray)
                    throw new ApplicationValidationException("Overclock effect thiếu predictions.");

                var effectResults = new BsonArray();
                foreach (var rawPrediction in rawPredictions.AsBsonArray)
                {
                    predictionCount++;
                    var prediction = rawPrediction.AsBsonDocument;
                    var targetTeamId = ParseGuid(prediction, "targetTeamId");
                    var boothId = ParseGuid(prediction, "boothId");
                    string outcome;
                    var ownerDelta = 0;
                    var targetDelta = 0;
                    if (!outcomeLookup.TryGetValue((targetTeamId, boothId), out var finalized))
                    {
                        outcome = "not_evaluated";
                        notEvaluatedCount++;
                    }
                    else if (finalized.SubmittedPoints == 0)
                    {
                        outcome = "correct";
                        correctCount++;
                        ownerDelta = effect.Data.GetInt("cdSteal", 15);
                        targetDelta = -ownerDelta;
                    }
                    else
                    {
                        outcome = "incorrect";
                        incorrectCount++;
                        ownerDelta = -effect.Data.GetInt("cdSelfPenalty", 5);
                    }

                    AddDelta(scoreAdjustments, ownerTeamId, ownerDelta);
                    AddDelta(scoreAdjustments, targetTeamId, targetDelta);
                    effectResults.Add(new BsonDocument
                    {
                        ["targetTeamId"] = targetTeamId.ToString(),
                        ["boothId"] = boothId.ToString(),
                        ["outcome"] = outcome,
                        ["ownerDelta"] = ownerDelta,
                        ["targetDelta"] = targetDelta
                    });
                }

                resolutions.Add(new CardEffectResolution(
                    effect.Id,
                    "predictions_resolved",
                    new BsonDocument
                    {
                        ["predictions"] = effectResults,
                        ["resolvedByEventId"] = eventId
                    }));
            }

            await unitOfWork.BeginAsync(cancellationToken);
            foreach (var (teamId, delta) in scoreAdjustments.Where(item => item.Value != 0))
            {
                var score = await sender.Send(new UpdateTeamScoreCommand
                {
                    RaceId = raceId,
                    TeamId = teamId,
                    Delta = delta,
                    Reason = "Chốt dự đoán Overclock",
                    PublishRealtimeNotification = false
                }, cancellationToken);
                if (score is null)
                    throw new ApplicationValidationException(
                        $"Không tìm thấy team '{teamId}' khi chốt Overclock.");
            }

            commitStarted = true;
            await unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            if (!commitStarted)
            {
                await cardRepository.ReleaseClaimedEffectsAsync(
                    raceId, eventId, DateTime.UtcNow, CancellationToken.None);
                await ReopenAfterSafeFailureAsync(
                    raceId, eventId, CancellationToken.None);
            }
            else
            {
                logger.LogCritical(
                    "SQL commit outcome is unknown for Overclock event {EventId}; Mongo claims were retained.",
                    eventId);
            }
            throw;
        }

        var mongoCompleted = await TryCompleteMongoAsync(
            raceId, eventId, actorId, now, resolutions);
        var windowResolved = mongoCompleted &&
            await TryMarkResolvedAsync(raceId, now, CancellationToken.None);

        foreach (var (teamId, delta) in scoreAdjustments.Where(item => item.Value != 0))
        {
            await notificationService.NotifyRaceScoreChangedAsync(
                raceId, teamId, delta, cancellationToken);
        }

        return new(
            windowResolved ? OverclockWindowStatus.Resolved : OverclockWindowStatus.Closed,
            predictionCount,
            correctCount,
            incorrectCount,
            notEvaluatedCount);
    }

    private async Task<bool> TryCompleteMongoAsync(
        Guid raceId,
        string eventId,
        Guid actorId,
        DateTime resolvedAt,
        IReadOnlyCollection<CardEffectResolution> resolutions)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await cardRepository.CompleteClaimedEffectsAsync(
                    raceId,
                    CardEffectEventCodes.OverclockResolution,
                    eventId,
                    actorId,
                    resolvedAt,
                    resolutions,
                    CancellationToken.None);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not complete Overclock Mongo state on attempt {Attempt}/3 for {EventId}.",
                    attempt,
                    eventId);
                if (attempt < 3)
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
            }
        }

        logger.LogCritical(
            "SQL Overclock changes committed but Mongo event {EventId} requires reconciliation.",
            eventId);
        return false;
    }

    private async Task ReopenAfterSafeFailureAsync(
        Guid raceId,
        string eventId,
        CancellationToken cancellationToken)
    {
        var document = await cardRepository.GetOrCreateAsync(raceId, cancellationToken);
        if (document.OverclockWindow.Status != OverclockWindowStatus.Closed ||
            document.OverclockWindow.ResolutionEventId != eventId)
            return;

        document.OverclockWindow.Status = OverclockWindowStatus.Open;
        document.OverclockWindow.ClosedAt = null;
        document.OverclockWindow.ClosedBy = null;
        document.OverclockWindow.ResolutionEventId = null;
        await cardRepository.ReplaceAsync(document, cancellationToken);
    }

    private async Task<bool> TryMarkResolvedAsync(
        Guid raceId,
        DateTime resolvedAt,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var document = await cardRepository.GetOrCreateAsync(raceId, cancellationToken);
                document.OverclockWindow.Status = OverclockWindowStatus.Resolved;
                document.OverclockWindow.ResolvedAt = resolvedAt;
                await cardRepository.ReplaceAsync(document, cancellationToken);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not mark Overclock window resolved on attempt {Attempt}/3.",
                    attempt);
                if (attempt < 3)
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
        }

        logger.LogCritical(
            "Overclock effects are resolved but race {RaceId} window still requires reconciliation.",
            raceId);
        return false;
    }

    private static Guid ParseGuid(BsonDocument document, string key) =>
        document.TryGetValue(key, out var value) && value.IsString &&
        Guid.TryParse(value.AsString, out var result) && result != Guid.Empty
            ? result
            : throw new ApplicationValidationException($"Overclock prediction thiếu {key} hợp lệ.");

    private static void AddDelta(IDictionary<Guid, int> values, Guid teamId, int delta)
    {
        if (delta != 0)
        {
            values.TryGetValue(teamId, out var current);
            values[teamId] = checked(current + delta);
        }
    }

    private static OverclockWindowResponse ToResponse(OverclockWindowState state) => new(
        state.Status,
        state.OpenedAt,
        state.OpenedBy,
        state.ClosedAt,
        state.ClosedBy,
        state.ResolvedAt,
        state.ResolutionEventId);
}
