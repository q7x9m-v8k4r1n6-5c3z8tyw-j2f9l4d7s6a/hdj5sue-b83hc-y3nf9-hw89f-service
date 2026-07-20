using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Constants;
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
            cancellationToken.ThrowIfCancellationRequested();

            return await _dapperHelper.QueryFirstOrDefaultAsync<Organizer>(
                OrganizerQueries.GetByEmailQuery(),
                new { Email = email },
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
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
            cancellationToken.ThrowIfCancellationRequested();

            await _dapperHelper.ExecuteAsync(
                OrganizerQueries.AddOrganizerQuery(),
                organizer,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
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
            cancellationToken.ThrowIfCancellationRequested();

            var result = await _dapperHelper.QueryAsync<Organizer>(
                OrganizerQueries.GetAllOrganizersQuery(),
                cancellationToken: cancellationToken);

            return result.ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
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
            cancellationToken.ThrowIfCancellationRequested();

            var parameters = new { Keyword = $"%{keyword}%" };
            var result = await _dapperHelper.QueryAsync<Organizer>(
                OrganizerQueries.SearchOrganizerQuery(),
                parameters,
                cancellationToken: cancellationToken);

            return result.ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching organizers with keyword {Keyword}.", keyword);
            throw;
        }
    }

    public async Task<bool> ChangeStatusAsync(
        Guid organizerId,
        string status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var organizer = await _dapperHelper.QueryFirstOrDefaultAsync<Organizer>(
                OrganizerQueries.GetOrganizerByIdQuery(),
                new { OrganizerId = organizerId },
                cancellationToken: cancellationToken);

            if (organizer is null)
            {
                return false;
            }

            await _dapperHelper.ExecuteAsync(
                OrganizerQueries.UpdateOrganizerStatusQuery(),
                new { OrganizerId = organizerId, Status = status },
                cancellationToken: cancellationToken);

            await _dapperHelper.ExecuteAsync(
                OrganizerQueries.UpdateOrganizerUserStatusQuery(),
                new
                {
                    OrganizerId = organizerId,
                    OrganizerRole = UserConstant.Role.Organizer,
                    UserStatus = status == OrganizerConstants.OrganizerStatus.Active
                        ? UserConstant.Status.Active
                        : UserConstant.Status.Deactive
                },
                cancellationToken: cancellationToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while changing organizer {OrganizerId} status to {Status}.",
                organizerId,
                status);
            throw;
        }
    }
}
