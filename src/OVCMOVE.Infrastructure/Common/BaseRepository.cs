using Microsoft.Extensions.Logging;
using OVCMOVE.Infrastructure.Helpers;

namespace OVCMOVE.Infrastructure.Common;

public abstract class BaseRepository<T>
{
    protected readonly ILogger<T> _logger;
    protected readonly IDapperHelper _dapperHelper;

    protected BaseRepository(ILogger<T> logger, IDapperHelper dapperHelper)
    {
        _logger = logger;
        _dapperHelper = dapperHelper;
    }
}
