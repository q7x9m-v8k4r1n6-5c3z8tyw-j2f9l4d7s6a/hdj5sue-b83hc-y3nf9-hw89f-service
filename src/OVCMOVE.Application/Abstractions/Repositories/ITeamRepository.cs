using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface ITeamRepository
{
    Task<List<Team>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Team>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}