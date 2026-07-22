using System;
using System.Threading;
using System.Threading.Tasks;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IBoothRepository
{
    Task<Guid?> CreateAsync(Booth booth, CancellationToken cancellationToken = default);
    Task DeleteByRaceIdAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<Booth?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SubmitScoreAndReleaseAsync(Guid boothId, Guid teamId, string organizerId, int score, CancellationToken cancellationToken = default);
}