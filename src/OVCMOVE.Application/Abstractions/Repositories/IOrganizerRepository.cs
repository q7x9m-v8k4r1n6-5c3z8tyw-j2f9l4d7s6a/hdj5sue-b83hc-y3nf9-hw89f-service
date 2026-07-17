using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IOrganizerRepository
{
    Task<List<Organizer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Organizer>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}