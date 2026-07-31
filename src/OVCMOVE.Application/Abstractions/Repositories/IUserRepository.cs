using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAnyStatusAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAnyStatusAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByShortNameAsync(string shortName, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateDisplayNameAsync(Guid id, string displayName, CancellationToken cancellationToken = default);
    Task UpdateGoogleProfileAsync(Guid id, string? displayName, string? avatarUrl, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(
        Guid id,
        string userType,
        string modifiedBy,
        DateTime modifiedAt,
        CancellationToken cancellationToken = default);
}
