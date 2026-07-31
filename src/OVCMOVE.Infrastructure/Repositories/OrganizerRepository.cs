using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.Features.Organizers.Query.GetAllOrganizers;
using OVCMOVE.Domain.Constants;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Persistence.Dapper;
using OVCMOVE.Infrastructure.Persistence.Queries;

namespace OVCMOVE.Infrastructure.Repositories;

public class OrganizerRepository : IOrganizerRepository
{
    private readonly IDbExecutor _db;

    public OrganizerRepository(IDbExecutor db) =>
        _db = db;

    public async Task<IReadOnlyCollection<Guid>> GetExistingIdsAsync(
        IEnumerable<Guid> organizerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = organizerIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var existingIds = await _db.QueryAsync<Guid>(
            OrganizerQueries.GetExistingIdsQuery(),
            new
            {
                Ids = ids,
                UserType = UserConstants.UserType.Organizer
            },
            cancellationToken: cancellationToken);
        return existingIds.ToArray();
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.QueryFirstOrDefaultAsync<User>(
            OrganizerQueries.GetByEmailQuery(),
            new
            {
                LinkedEmail = email,
                UserType = UserConstants.UserType.Organizer
            },
            cancellationToken: cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid organizerId, CancellationToken cancellationToken = default) =>
        _db.QueryFirstOrDefaultAsync<User>(
            OrganizerQueries.GetOrganizerByIdQuery(),
            new { OrganizerId = organizerId, UserType = UserConstants.UserType.Organizer },
            cancellationToken: cancellationToken);

    public async Task<(
        IReadOnlyCollection<GetAllOrganizersResultModel> Items,
        int TotalItems)> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _db.QueryAsync<GetAllOrganizersResultModel>(
            OrganizerQueries.GetAllOrganizersQuery(),
            new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                UserType = UserConstants.UserType.Organizer,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken);
        var totalItems = await _db.QueryFirstOrDefaultAsync<int>(
            OrganizerQueries.CountOrganizersQuery(),
            new { UserType = UserConstants.UserType.Organizer, Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim() },
            cancellationToken: cancellationToken);
        return (result.ToArray(), totalItems);
    }

    public async Task<IReadOnlyCollection<User>> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _db.QueryAsync<User>(
            OrganizerQueries.SearchOrganizerQuery(),
            new
            {
                Keyword = $"%{keyword}%",
                UserType = UserConstants.UserType.Organizer
            },
            cancellationToken: cancellationToken);
        return result.ToArray();
    }

    public async Task<bool> ChangeStatusAsync(
        Guid organizerId,
        string status,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var affectedRows = await _db.ExecuteAsync(
            OrganizerQueries.UpdateOrganizerStatusQuery(),
            new
            {
                OrganizerId = organizerId,
                UserType = UserConstants.UserType.Organizer,
                UserStatus = status == UserConstants.Status.Active
                    ? UserConstants.Status.Active
                    : UserConstants.Status.Inactive,
                ModifiedBy = "system",
                ModifiedAt = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);
        return affectedRows >= 1;
    }

    public async Task<bool> UpdateAsync(User organizer, CancellationToken cancellationToken = default) =>
        await _db.ExecuteAsync(OrganizerQueries.UpdateOrganizerQuery(), new
        {
            organizer.Id, organizer.DisplayName, organizer.Status, organizer.ModifiedBy, organizer.ModifiedAt,
            UserType = UserConstants.UserType.Organizer,
        }, cancellationToken: cancellationToken) == 1;
}
