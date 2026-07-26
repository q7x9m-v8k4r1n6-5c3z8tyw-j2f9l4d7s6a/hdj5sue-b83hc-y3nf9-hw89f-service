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

    private readonly record struct ConnectionScope(IDbConnection Connection, IDbTransaction? Transaction, bool DisposeConnection);

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
        var scope = await OpenConnectionAsync(transaction);

        try
        {
            var command = new CommandDefinition(sql, param, scope.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await scope.Connection.QueryAsync<T>(command);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(transaction);

        try
        {
            var command = new CommandDefinition(sql, param, scope.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await scope.Connection.QueryFirstOrDefaultAsync<T>(command);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    public async Task<T> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(transaction);

        try
        {
            var command = new CommandDefinition(sql, param, scope.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await scope.Connection.QuerySingleAsync<T>(command);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(transaction);

        try
        {
            var command = new CommandDefinition(sql, param, scope.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await scope.Connection.ExecuteAsync(command);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(transaction);

        try
        {
            var command = new CommandDefinition(sql, param, scope.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
            return await scope.Connection.ExecuteScalarAsync<T>(command);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    private async Task<ConnectionScope> OpenConnectionAsync(IDbTransaction? transaction)
    {
        var effectiveTransaction = transaction ?? _unitOfWork.Transaction;
        var transactionConnection = effectiveTransaction?.Connection;

        if (transactionConnection is not null)
        {
            await EnsureConnectionOpenAsync(transactionConnection);
            return new ConnectionScope(transactionConnection, effectiveTransaction, false);
        }

        var connection = _sqlServerFactory.CreateConnection();
        await EnsureConnectionOpenAsync(connection);
        return new ConnectionScope(connection, null, true);
    }

    private static async Task EnsureConnectionOpenAsync(IDbConnection connection)
    {
        if (connection.State == ConnectionState.Open)
        {
            return;
        }

        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync();
            return;
        }

        connection.Open();
    }

    private static void DisposeConnection(ConnectionScope scope)
    {
        if (!scope.DisposeConnection)
        {
            return;
        }

        scope.Connection.Dispose();
    }
}