using OVCMOVE.Domain.Entities;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Application.Features.Teams.Query.GetAllTeams;
using OVCMOVE.Application.Features.Teams.Query.SearchTeam;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface ITeamRepository
{
    Task<List<GetAllTeamsResultModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<SearchTeamResultModel>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<List<GetTeamLeaderboardResultModel>> GetLeaderboardAsync(CancellationToken cancellationToken = default);
}