using OVCMOVE.Application.DTOs.Security;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyCollection<PermissionSummaryModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Guid?> CreateAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid permissionId, string modifiedBy, DateTime modifiedAt, CancellationToken cancellationToken = default);
}
