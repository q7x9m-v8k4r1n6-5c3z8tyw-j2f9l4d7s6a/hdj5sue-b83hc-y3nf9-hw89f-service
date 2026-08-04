using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreCommandHandler : IRequestHandler<SubmitBoothScoreCommand, bool>
{
    private readonly IBoothRepository _boothRepository;
    private readonly IBoothNotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitBoothScoreCommandHandler(
        IBoothRepository boothRepository,
        IBoothNotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _boothRepository = boothRepository;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SubmitBoothScoreCommand request, CancellationToken cancellationToken)
    {
        var model = new SubmitBoothScoreModel
        {
            BoothId = request.BoothID,
            TeamId = request.TeamID,
            OrganizerId = request.OrganizerId,
            Score = request.Score
        };

        await _unitOfWork.BeginAsync(cancellationToken);
        try
        {
            var booth = await _boothRepository.GetByIdAsync(request.BoothID, cancellationToken);
            var result = await _boothRepository.SubmitScoreAndReleaseAsync(model, cancellationToken);
            if (!result)
            {
                await _unitOfWork.RollbackAsync(CancellationToken.None);
                return false;
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            if (booth is not null)
            {
                await _notificationService.NotifyRaceScoreChangedAsync(
                    booth.RaceId,
                    request.TeamID,
                    request.Score,
                    cancellationToken);
            }

            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
