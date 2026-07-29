using MediatR;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

namespace OVCMOVE.Application.Features.Teams.Query.GetTeamLeaderboard;

public class TeamLeaderboardQueryHandler : 
    IRequestHandler<TeamLeaderboardQuery, List<TeamLeaderboardResultModel>>
{
    private readonly ITeamRepository _teamRepository;

    public TeamLeaderboardQueryHandler(
        ITeamRepository teamRepository) 
    {
        _teamRepository = teamRepository;
    }

    public async Task<List<TeamLeaderboardResultModel>> Handle(
        TeamLeaderboardQuery request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _teamRepository.GetLeaderboardAsync(request.RaceId, cancellationToken);
    }
}