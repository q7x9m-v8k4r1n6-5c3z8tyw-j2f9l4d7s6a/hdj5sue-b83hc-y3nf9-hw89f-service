using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IOrganizerRepository
{
    Task<Organizer?> GetByEmailAsync(string email);
    Task AddAsync(Organizer organizer);
}