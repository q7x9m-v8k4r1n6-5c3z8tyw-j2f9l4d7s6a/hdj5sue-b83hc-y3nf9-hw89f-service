using MediatR;

namespace OVCMOVE.Application.Features.Teams.Query.TeamLeaderboard;

public sealed record TeamLeaderboardQuery(
    Guid RaceId,
    Guid TeamId) : IRequest<TeamLeaderboardResultModel>;
