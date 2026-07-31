using MediatR;

using OVCMOVE.Application.Abstractions.Repositories;

namespace OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;

public class TeamLeaderboardQueryHandler : 
    IRequestHandler<TeamLeaderboardQuery, List<TeamLeaderboardResultModel>>
{
    private readonly IRaceRepository _raceRepository;

    public TeamLeaderboardQueryHandler(
        IRaceRepository raceRepository) 
    {
        _raceRepository = raceRepository;
    }

    public async Task<List<TeamLeaderboardResultModel>> Handle(
        TeamLeaderboardQuery request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _raceRepository.GetLeaderboardAsync(request.RaceId, cancellationToken);
    }
}