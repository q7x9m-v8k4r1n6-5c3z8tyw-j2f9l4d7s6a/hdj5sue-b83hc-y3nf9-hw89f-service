using System.Data;
using System.Data.Common;
using System.Threading;
using Dapper;
using OVCMOVE.Infrastructure.Persistance.SqlServer;

namespace OVCMOVE.Infrastructure.Helpers;

public class DapperHelper : IDapperHelper
{
    private readonly ISqlServerFactory _sqlServerFactory;

    public DapperHelper(ISqlServerFactory sqlServerFactory)
    {
        _sqlServerFactory = sqlServerFactory;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await OpenConnectionAsync();

        var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.QueryAsync<T>(command);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await OpenConnectionAsync();

        var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<T>(command);
    }

    public async Task<T> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await OpenConnectionAsync();

        var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<T>(command);
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await OpenConnectionAsync();

        var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = await OpenConnectionAsync();

        var command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<T>(command);
    }

    private async Task<IDbConnection> OpenConnectionAsync()
    {
        var connection = _sqlServerFactory.CreateConnection();

        if (connection.State == ConnectionState.Open)
        {
            return connection;
        }

        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync();
            return dbConnection;
        }

        connection.Open();
        return connection;
    }
}
