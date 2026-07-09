using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class OrganizerRepository : BaseRepository<OrganizerRepository>, IOrganizerRepository
{
    public OrganizerRepository(ILogger<OrganizerRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<Organizer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dapperHelper.QueryFirstOrDefaultAsync<Organizer>(
                OrganizerQueryHelper.GetByEmailQuery(),
                new { Email = email },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting organizer by email {Email}.", email);
            throw;
        }
    }

    public async Task AddAsync(Organizer organizer, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dapperHelper.ExecuteAsync(
                OrganizerQueryHelper.AddOrganizerQuery(),
                organizer,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding organizer with email {Email}.", organizer.Email);
            throw;
        }
    }
}
