using Microsoft.Extensions.Logging;

using OVCMOVE.Application.Abstractions.Repositories;
using OVCMOVE.Domain.Entities; 
using OVCMOVE.Domain.Constants; 
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Helpers;
using OVCMOVE.Infrastructure.Helpers.QueriesHelper;

namespace OVCMOVE.Infrastructure.Repositories;

public class BoothScoringLogsRepository : BaseRepository<UserRepository>, IBoothScoringLogsRepository
{
    public BoothScoringLogsRepository(ILogger<UserRepository> logger, IDapperHelper dapperHelper) 
        : base(logger, dapperHelper)
    {
    }


}
