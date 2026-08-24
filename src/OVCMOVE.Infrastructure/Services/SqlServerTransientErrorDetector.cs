using System.Data.Common;
using Microsoft.Data.SqlClient;
using OVCMOVE.Application.Abstractions.Services;

namespace OVCMOVE.Infrastructure.Services;

public sealed class SqlServerTransientErrorDetector : ITransientErrorDetector
{
    private static readonly HashSet<int> TransientSqlErrorNumbers =
    [
        -2,
        20,
        64,
        233,
        1205,
        4060,
        10928,
        10929,
        40197,
        40501,
        40613,
        49918,
        49919,
        49920,
        10053,
        10054,
        10060
    ];

    public bool IsTransient(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;
            if (current is SqlException sqlException &&
                sqlException.Errors.Cast<SqlError>().Any(error =>
                    TransientSqlErrorNumbers.Contains(error.Number)))
                return true;
            if (current is DbException { IsTransient: true })
                return true;
        }

        return false;
    }
}
