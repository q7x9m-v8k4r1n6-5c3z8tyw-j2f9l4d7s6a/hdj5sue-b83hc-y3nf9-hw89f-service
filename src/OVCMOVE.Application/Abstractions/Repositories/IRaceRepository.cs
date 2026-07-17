using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRaceRepository
{
    Task<Guid?> CreateAsync(Race race, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RaceListItemResultModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RaceDetailResultModel?> GetDetailAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<Race?> GetByIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Race race, CancellationToken cancellationToken = default);
}
