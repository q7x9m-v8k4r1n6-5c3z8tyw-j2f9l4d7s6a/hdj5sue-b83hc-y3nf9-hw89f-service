using System.Data;
using System.Data.Common;
using Dapper;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure.Persistance.SqlServer;

namespace OVCMOVE.Infrastructure.Helpers;

public class DapperHelper : IDapperHelper
{
    private readonly ISqlServerFactory _sqlServerFactory;
    private readonly IUnitOfWork _unitOfWork;

    public DapperHelper(ISqlServerFactory sqlServerFactory, IUnitOfWork unitOfWork)
    {
        _sqlServerFactory = sqlServerFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithConnectionAsync(
            transaction,
            async (connection, effectiveTransaction) =>
            {
                var command = new CommandDefinition(sql, param, effectiveTransaction, commandTimeout, commandType, cancellationToken: cancellationToken);
                return await connection.QueryAsync<T>(command);
            });
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithConnectionAsync(
            transaction,
            async (connection, effectiveTransaction) =>
            {
                var command = new CommandDefinition(sql, param, effectiveTransaction, commandTimeout, commandType, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<T>(command);
            });
    }

    public async Task<T> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithConnectionAsync(
            transaction,
            async (connection, effectiveTransaction) =>
            {
                var command = new CommandDefinition(sql, param, effectiveTransaction, commandTimeout, commandType, cancellationToken: cancellationToken);
                return await connection.QuerySingleAsync<T>(command);
            });
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithConnectionAsync(
            transaction,
            async (connection, effectiveTransaction) =>
            {
                var command = new CommandDefinition(sql, param, effectiveTransaction, commandTimeout, commandType, cancellationToken: cancellationToken);
                return await connection.ExecuteAsync(command);
            });
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithConnectionAsync(
            transaction,
            async (connection, effectiveTransaction) =>
            {
                var command = new CommandDefinition(sql, param, effectiveTransaction, commandTimeout, commandType, cancellationToken: cancellationToken);
                return await connection.ExecuteScalarAsync<T>(command);
            });
    }

    private async Task<TResult> ExecuteWithConnectionAsync<TResult>(
        IDbTransaction? transaction,
        Func<IDbConnection, IDbTransaction?, Task<TResult>> execute)
    {
        var effectiveTransaction = transaction ?? _unitOfWork.Transaction;
        if (effectiveTransaction?.Connection is not null)
        {
            return await execute(effectiveTransaction.Connection, effectiveTransaction);
        }

        using var connection = await OpenConnectionAsync();
        return await execute(connection, null);
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
