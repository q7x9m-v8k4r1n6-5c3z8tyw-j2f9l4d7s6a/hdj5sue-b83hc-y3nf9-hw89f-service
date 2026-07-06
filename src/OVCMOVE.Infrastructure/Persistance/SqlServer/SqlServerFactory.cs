using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Persistance.SqlServer;

public class SqlServerFactory : ISqlServerFactory
{
    private readonly DbConfigOptions _databaseConfigOptions;

    public SqlServerFactory(IOptions<DbConfigOptions> databaseConfigOptions)
    {
        _databaseConfigOptions = databaseConfigOptions.Value;
    }

    public IDbConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_databaseConfigOptions.SQLServer.ConnectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DbConfig:SQLServer:ConnectionString' is not configured.");
        }

        return new SqlConnection(_databaseConfigOptions.SQLServer.ConnectionString);
    }
}
