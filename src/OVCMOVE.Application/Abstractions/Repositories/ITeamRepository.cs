using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<Team?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Team?> GetByLeaderEmailAsync(string leaderEmail, CancellationToken cancellationToken = default);
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task<List<Team>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Team>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}
