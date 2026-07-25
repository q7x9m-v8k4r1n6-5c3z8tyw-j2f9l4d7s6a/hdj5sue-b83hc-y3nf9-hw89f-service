using OVCMOVE.Application.DTOs.Team;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface ITeamRepository
{
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task<List<TeamAccountDetails>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<TeamAccountDetails>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}
