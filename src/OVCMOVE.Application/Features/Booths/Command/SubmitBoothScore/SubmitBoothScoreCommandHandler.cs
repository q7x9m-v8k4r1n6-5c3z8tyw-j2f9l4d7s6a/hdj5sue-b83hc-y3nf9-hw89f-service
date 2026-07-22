using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Booths.Commands.SubmitBoothScore;

public class SubmitBoothScoreCommandHandler : IRequestHandler<SubmitBoothScoreCommand, bool>
{
    private readonly IBoothRepository _boothRepository;

    public SubmitBoothScoreCommandHandler(IBoothRepository boothRepository)
    {
        _boothRepository = boothRepository;
    }

    public async Task<bool> Handle(SubmitBoothScoreCommand request, CancellationToken cancellationToken)
    {
        return await _boothRepository.SubmitScoreAndReleaseAsync(
            request.BoothID,
            request.TeamID,
            request.OrganizerId,
            request.Score,
            cancellationToken
        );
    }
}