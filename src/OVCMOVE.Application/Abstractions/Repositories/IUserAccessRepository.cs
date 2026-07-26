using OVCMOVE.Application.DTOs.Security;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IUserAccessRepository
{
    Task<UserAccessProfileModel> GetAccessProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
