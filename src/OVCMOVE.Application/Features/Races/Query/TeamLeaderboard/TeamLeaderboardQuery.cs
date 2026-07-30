using MediatR;

namespace OVCMOVE.Application.Features.Races.Query.TeamLeaderboard;

public record TeamLeaderboardQuery : IRequest<List<TeamLeaderboardResultModel>>
{
    public Guid? RaceId { get; set; }
}