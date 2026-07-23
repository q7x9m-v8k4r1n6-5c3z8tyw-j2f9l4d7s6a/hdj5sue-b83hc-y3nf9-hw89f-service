using OVCMOVE.Application.DTOs.ResultModels;

namespace OVCMOVE.Application.Abstractions.Repositories;

public interface IBoothScoringLogRepository
{
    Task<List<GetBoothScoringLogsResultModel>> GetScoringLogsAsync(int? limit, CancellationToken cancellationToken = default);
}