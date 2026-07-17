using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IOrganizerRepository
{
    Task<OrganizerResultModel.ChangeOrganizerStatusResultModel> ChangeStatusAsync(
        int organizerId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
