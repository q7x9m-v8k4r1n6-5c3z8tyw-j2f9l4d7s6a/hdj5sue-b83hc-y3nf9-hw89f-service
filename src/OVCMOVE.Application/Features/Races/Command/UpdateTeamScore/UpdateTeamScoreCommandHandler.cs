using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Application.Common;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Domain.Constants;

namespace OVCMOVE.Application.Features.Races.Command.UpdateTeamScore;

public sealed class UpdateTeamScoreCommandHandler :
    IRequestHandler<UpdateTeamScoreCommand, UpdateTeamScoreResult?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTeamScoreCommandHandler(
        IRaceRepository raceRepository,
        IBoothNotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _raceRepository = raceRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateTeamScoreResult?> Handle(
        UpdateTeamScoreCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(request);

        var actor = request.GetActorOrSystem();
        var now = DateTime.UtcNow;
        var reason = request.Reason.Trim();
        var ownsTransaction = !_unitOfWork.HasActiveTransaction;

        try
        {
            if (ownsTransaction)
                await _unitOfWork.BeginAsync(cancellationToken);

            var scoreBefore = await _raceRepository.GetRaceTeamScoreAsync(
                request.RaceId,
                request.TeamId,
                cancellationToken);
            if (scoreBefore is null)
            {
                if (ownsTransaction)
                    await _unitOfWork.RollbackAsync(CancellationToken.None);
                return null;
            }

            var scoreAfter = scoreBefore.Value + request.Delta;
            var updated = await _raceRepository.UpdateRaceTeamScoreAsync(
                request.RaceId,
                request.TeamId,
                scoreAfter,
                actor,
                now,
                cancellationToken);
            if (!updated)
            {
                if (ownsTransaction)
                    await _unitOfWork.RollbackAsync(CancellationToken.None);
                return null;
            }

            await _raceRepository.CreateScoringLogAsync(
                new ScoringLog
                {
                    Id = Guid.NewGuid(),
                    EventCode = ScoringLogConstants.EventCode.ManualScoreAdjustment,
                    EventName = ScoringLogConstants.EventName.ManualScoreAdjustment,
                    RaceId = request.RaceId,
                    TeamId = request.TeamId,
                    ActorId = null,
                    BoothId = null,
                    Delta = request.Delta,
                    ScoreBefore = scoreBefore.Value,
                    ScoreAfter = scoreAfter,
                    ReasonCode = ScoringLogConstants.ReasonCode.Manual,
                    Reason = reason,
                    CreatedBy = actor,
                    CreatedAt = now,
                    ModifiedBy = actor,
                    ModifiedAt = now,
                    IsDeleted = false
                },
                cancellationToken);

            if (ownsTransaction)
                await _unitOfWork.CommitAsync(cancellationToken);
            if (ownsTransaction && request.PublishRealtimeNotification)
            {
                await _notificationService.NotifyRaceScoreChangedAsync(
                    request.RaceId,
                    request.TeamId,
                    request.Delta,
                    cancellationToken);
            }

            return new UpdateTeamScoreResult(
                request.RaceId,
                request.TeamId,
                scoreBefore.Value,
                scoreAfter,
                request.Delta);
        }
        catch
        {
            if (ownsTransaction)
                await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void Validate(UpdateTeamScoreCommand request)
    {
        if (request.RaceId == Guid.Empty)
        {
            throw new ApplicationValidationException("RaceId is required.");
        }

        if (request.TeamId == Guid.Empty)
        {
            throw new ApplicationValidationException("TeamId is required.");
        }

        if (request.Delta == 0)
        {
            throw new ApplicationValidationException("Delta must be different from zero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ApplicationValidationException("Reason is required.");
        }
    }
}
