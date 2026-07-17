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
                OrganizerQueries.GetByEmailQuery(),
                new { Email = email });
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
                OrganizerQueries.AddOrganizerQuery(),
                organizer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding organizer with email {Email}.", organizer.Email);
            throw;
        }
    }

    public async Task<List<Organizer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dapperHelper.QueryAsync<Organizer>(
                OrganizerQueries.GetAllOrganizersQuery());

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all organizers.");
            throw;
        }
    }

    public async Task<List<Organizer>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new { Keyword = $"%{keyword}%" };
            var result = await _dapperHelper.QueryAsync<Organizer>(
                OrganizerQueries.SearchOrganizerQuery(),
                parameters);

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching organizers with keyword {Keyword}.", keyword);
            throw;
        }
    }
}
