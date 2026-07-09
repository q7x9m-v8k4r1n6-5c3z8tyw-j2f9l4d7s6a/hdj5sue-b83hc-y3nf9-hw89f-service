using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IOrganizerRepository
{
    Task<Organizer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Organizer organizer, CancellationToken cancellationToken = default);
}