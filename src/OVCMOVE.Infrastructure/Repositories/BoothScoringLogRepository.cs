using Microsoft.Extensions.Logging;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Application.DTOs.ResultModels;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;
using OVCMOVE.Infrastructure.Helpers;

namespace OVCMOVE.Infrastructure.Repositories;

public class BoothScoringLogRepository : BaseRepository<BoothScoringLogRepository>, IBoothScoringLogRepository
{
    public BoothScoringLogRepository(ILogger<BoothScoringLogRepository> logger, IDapperHelper dapperHelper)
        : base(logger, dapperHelper)
    {
    }

    public async Task<List<GetBoothScoringLogsResultModel>> GetScoringLogsAsync(int? limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string sqlQuery = BoothScoringLogQueries.GetScoringLogsQuery(limit);
        
        var result = await _dapperHelper.QueryAsync<GetBoothScoringLogsResultModel>(sqlQuery);
        
        return result.ToList();
    }
}