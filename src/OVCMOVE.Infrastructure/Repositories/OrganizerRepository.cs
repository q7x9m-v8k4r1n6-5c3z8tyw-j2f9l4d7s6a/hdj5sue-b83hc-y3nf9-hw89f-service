using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;
using OVCMOVE.Domain.Entities;

namespace OVCMOVE.Infrastructure.Repositories;

public class OrganizerRepository : BaseRepository<OrganizerRepository>, IOrganizerRepository
{
    public OrganizerRepository(ILogger<OrganizerRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<List<Organizer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sqlQuery = OrganizerQueries.GetAllOrganizersQuery();
            var result = await _dapperHelper.QueryAsync<Organizer>(sqlQuery);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error when getting all organizers");
            throw;
        }
    }

    public async Task<List<Organizer>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sqlQuery = OrganizerQueries.SearchOrganizerQuery();
            var parameters = new { Keyword = $"%{keyword}%" };
            var result = await _dapperHelper.QueryAsync<Organizer>(sqlQuery, parameters);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error when searching organizer");
            throw;
        }
    }
}