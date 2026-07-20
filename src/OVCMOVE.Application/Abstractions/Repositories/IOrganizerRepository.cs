using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IOrganizerRepository
{
    Task<Organizer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Organizer organizer, CancellationToken cancellationToken = default);
    Task<List<Organizer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<Organizer>> SearchAsync(string keyword, CancellationToken cancellationToken = default);

    Task<bool> ChangeStatusAsync(Guid organizerId, string status, CancellationToken cancellationToken = default);
}
