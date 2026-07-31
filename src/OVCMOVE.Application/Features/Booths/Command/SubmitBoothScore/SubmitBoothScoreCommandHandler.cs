using MediatR;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreCommandHandler : IRequestHandler<SubmitBoothScoreCommand, bool>
{
    private readonly IBoothRepository _boothRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitBoothScoreCommandHandler(
        IBoothRepository boothRepository,
        IUnitOfWork unitOfWork)
    {
        _boothRepository = boothRepository;
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
            var result = await _boothRepository.SubmitScoreAndReleaseAsync(model, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}