using MediatR;

namespace OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

public record TeamLeaderboardQuery : IRequest<List<TeamLeaderboardResultModel>>
{
    public Guid? RaceId { get; set; }
}