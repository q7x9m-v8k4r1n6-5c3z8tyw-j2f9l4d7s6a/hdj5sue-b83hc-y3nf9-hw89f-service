using Microsoft.Extensions.Logging;
using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;
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

    public async Task<OrganizerResultModel.ChangeOrganizerStatusResultModel> ChangeStatusAsync(
        int organizerId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var organizer = await _dapperHelper.QueryFirstOrDefaultAsync<OrganizerStatusRecord>(
            OrganizerQueryHelper.GetOrganizerStatusByIdQuery(),
            new { OrganizerId = organizerId });

        if (organizer is null)
        {
            return new OrganizerResultModel.ChangeOrganizerStatusResultModel
            {
                OrganizerId = organizerId,
                IsActive = isActive,
                IsSuccess = false,
                Message = "Organizer account was not found."
            };
        }

        if (organizer.IsActive == isActive)
        {
            return new OrganizerResultModel.ChangeOrganizerStatusResultModel
            {
                OrganizerId = organizerId,
                IsActive = organizer.IsActive,
                IsSuccess = false,
                Message = isActive
                    ? "Organizer account is already active."
                    : "Organizer account is already deactivated."
            };
        }

        await _dapperHelper.ExecuteAsync(
            OrganizerQueryHelper.UpdateOrganizerStatusQuery(),
            new
            {
                OrganizerId = organizerId,
                IsActive = isActive
            });

        return new OrganizerResultModel.ChangeOrganizerStatusResultModel
        {
            OrganizerId = organizerId,
            IsActive = isActive,
            IsSuccess = true,
            Message = isActive
                ? "Organizer account has been activated successfully."
                : "Organizer account has been deactivated successfully."
        };
    }

    private sealed class OrganizerStatusRecord
    {
        public int Id { get; init; }

        public bool IsActive { get; init; }
    }
}
