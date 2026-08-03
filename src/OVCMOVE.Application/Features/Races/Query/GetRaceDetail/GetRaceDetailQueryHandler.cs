using MediatR;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Races.Query.GetRaceDetail;

public class GetRaceDetailQueryHandler :
    IRequestHandler<GetRaceDetailQuery, RaceDetailResultModel?>
{
    private readonly IRaceRepository _raceRepository;
    private readonly IRaceTeamRepository _raceTeamRepository;

    public GetRaceDetailQueryHandler(
        IRaceRepository raceRepository,
        IRaceTeamRepository raceTeamRepository)
    {
        _raceRepository = raceRepository;
        _raceTeamRepository = raceTeamRepository;
    }

    /// <summary>Returns the complete race view or null when the race is missing.</summary>
    public async Task<RaceDetailResultModel?> Handle(GetRaceDetailQuery request, CancellationToken cancellationToken)
    {
        if (request.TeamId.HasValue)
        {
            var assignedTeamIds = await _raceTeamRepository.GetTeamIdsByRaceIdAsync(
                request.RaceId,
                cancellationToken);
            if (!assignedTeamIds.Contains(request.TeamId.Value))
            {
                return null;
            }
        }

        return await _raceRepository.GetDetailAsync(request.RaceId, cancellationToken);
    }
}
