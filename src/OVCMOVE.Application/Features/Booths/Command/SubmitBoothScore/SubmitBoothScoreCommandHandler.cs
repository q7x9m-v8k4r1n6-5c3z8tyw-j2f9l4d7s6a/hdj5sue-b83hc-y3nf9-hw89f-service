using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.Features.Booths.Common;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.Abstractions.Plugins;

namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreCommandHandler : IRequestHandler<SubmitBoothScoreCommand, bool>
{
    private readonly IBoothRepository _boothRepository;
    private readonly IBoothNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRaceRepository _raceRepository;
    private readonly IBoothOrganizerRepository _boothOrganizerRepository;
    private readonly IPluginHub _pluginHub;

    public SubmitBoothScoreCommandHandler(
        IBoothRepository boothRepository,
        IBoothNotificationService notificationService,
        IUnitOfWork unitOfWork,
        IRaceRepository raceRepository,
        IBoothOrganizerRepository boothOrganizerRepository,
        IPluginHub pluginHub)
    {
        _boothRepository = boothRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _raceRepository = raceRepository;
        _boothOrganizerRepository = boothOrganizerRepository;
        _pluginHub = pluginHub;
    }

    public async Task<bool> Handle(SubmitBoothScoreCommand request, CancellationToken cancellationToken)
    {
        var completionId = Guid.NewGuid();
        var model = new SubmitBoothScoreModel
        {
            CompletionId = completionId,
            BoothId = request.BoothID,
            TeamId = request.TeamID,
            OrganizerId = request.OrganizerId,
            Score = request.Score
        };

        Booth? booth;
        bool result;
        BoothResultFinalizedData? finalizedData = null;

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            booth = await _boothRepository.GetByIdAsync(request.BoothID, cancellationToken);
            if (booth is null)
            {
                throw new ApplicationNotFoundException("Trạm thi đấu không tồn tại.");
            }

            var isAssigned = await _boothOrganizerRepository.IsAssignedAsync(
                request.OrganizerId,
                request.BoothID,
                cancellationToken);
            if (!isAssigned)
            {
                throw new ApplicationForbiddenException(
                    "Bạn không được phân công quản lý trạm này.");
            }

            if (booth.Status != BoothConstants.BoothStatus.Occupied ||
                booth.TeamId != request.TeamID)
            {
                throw new ApplicationConflictException(
                    "Đội không còn chiếm trạm này.");
            }

            var progress = await _raceRepository.GetBoothProgressAsync(
                booth.RaceId,
                request.TeamID,
                request.BoothID,
                cancellationToken);
            var entryError = BoothParticipationPolicy.GetEntryError(booth, progress);
            if (entryError is not null)
            {
                throw new ApplicationValidationException(entryError);
            }

            result = await _boothRepository.SubmitScoreAndReleaseAsync(model, cancellationToken);
            if (!result)
                throw new ApplicationConflictException(
                    "Kết quả booth đã được xử lý bởi yêu cầu khác.");

            finalizedData = new BoothResultFinalizedData
            {
                BoothCompletionId = completionId,
                BoothType = booth.Type,
                BoothMaximumScore = booth.MaximumScore,
                SubmittedPoints = request.Score,
                FinalAwardedPoints = request.Score,
                Result = request.Score == 0
                    ? BoothResultValues.Failed
                    : BoothResultValues.Succeeded
            };
            await _pluginHub.DispatchAsync(
                new PluginEventContext(
                    PluginEventNames.BoothResultFinalized,
                    booth.RaceId,
                    request.TeamID,
                    request.BoothID,
                    DateTime.UtcNow,
                    $"booth-result:{completionId:D}",
                    finalizedData),
                cancellationToken);
            await _unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        if (result)
        {
            var scoreChanges = new Dictionary<Guid, int>
            {
                [request.TeamID] = request.Score
            };
            foreach (var adjustment in finalizedData!.ScoreAdjustments)
            {
                scoreChanges[adjustment.TeamId] =
                    scoreChanges.GetValueOrDefault(adjustment.TeamId) + adjustment.Delta;
            }
            foreach (var (teamId, delta) in scoreChanges)
            {
                await _notificationService.NotifyRaceScoreChangedAsync(
                    booth.RaceId,
                    teamId,
                    delta,
                    cancellationToken);
            }

            await _notificationService.NotifyBoothStatusChangedAsync(
                booth.RaceId,
                request.BoothID,
                BoothConstants.BoothStatus.Free,
                null,
                null,
                cancellationToken);
        }

        return result;
    }
}
