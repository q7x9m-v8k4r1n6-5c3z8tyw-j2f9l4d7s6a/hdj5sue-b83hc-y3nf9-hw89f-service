using MediatR;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Features.Teams.Query.GetTeamLeaderboard;

public class GetTeamLeaderboardQuery : IRequest<List<GetTeamLeaderboardResultModel>>
{
}