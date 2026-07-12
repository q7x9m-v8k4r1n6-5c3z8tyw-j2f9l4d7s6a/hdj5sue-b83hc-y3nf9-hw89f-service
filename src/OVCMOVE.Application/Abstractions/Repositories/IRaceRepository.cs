using OVCMOVE.Application.DTOs.RequestModels;
using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IRaceRepository
{
    Task<RaceResultModel.AdminRaceListResultModel> GetAdminRaceListAsync(CancellationToken cancellationToken = default);

    Task<RaceResultModel.AdminRaceDetailResultModel?> GetAdminRaceDetailAsync(int raceId, CancellationToken cancellationToken = default);

    Task<RaceResultModel.AdminRaceDetailResultModel> CreateAsync(RaceWriteModel race, CancellationToken cancellationToken = default);

    Task<RaceResultModel.AdminRaceDetailResultModel?> UpdateAsync(int raceId, RaceWriteModel race, CancellationToken cancellationToken = default);
}
