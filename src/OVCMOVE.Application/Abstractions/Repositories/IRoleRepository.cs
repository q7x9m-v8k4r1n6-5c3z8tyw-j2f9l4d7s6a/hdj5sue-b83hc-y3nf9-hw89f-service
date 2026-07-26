using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<RoleSummaryModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Guid?> CreateAsync(Role role, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid roleId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default);
}
