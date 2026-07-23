using MediatR;
using Microsoft.Extensions.Logging;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Common;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Teams.Query.GetTeamLeaderboard;

public class GetTeamLeaderboardQueryHandler : 
    BaseQueryHandler<GetTeamLeaderboardQueryHandler>,
    IRequestHandler<GetTeamLeaderboardQuery, List<GetTeamLeaderboardResultModel>>
{
    private readonly ITeamRepository _teamRepository;

    public GetTeamLeaderboardQueryHandler(
        ILogger<GetTeamLeaderboardQueryHandler> logger,
        ITeamRepository teamRepository) : base(logger)
    {
        _teamRepository = teamRepository;
    }

    public async Task<List<GetTeamLeaderboardResultModel>> Handle(GetTeamLeaderboardQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return await _teamRepository.GetLeaderboardAsync(cancellationToken);
    }
}