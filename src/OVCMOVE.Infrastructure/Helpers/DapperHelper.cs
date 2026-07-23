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
        var context = await GetCommandContextAsync(transaction);
        using var disposableConnection = context.ShouldDisposeConnection ? context.Connection : null;
        var command = new CommandDefinition(sql, param, context.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await context.Connection.QueryAsync<T>(command);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCommandContextAsync(transaction);
        using var disposableConnection = context.ShouldDisposeConnection ? context.Connection : null;
        var command = new CommandDefinition(sql, param, context.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await context.Connection.QueryFirstOrDefaultAsync<T>(command);
    }

    public async Task<T> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCommandContextAsync(transaction);
        using var disposableConnection = context.ShouldDisposeConnection ? context.Connection : null;
        var command = new CommandDefinition(sql, param, context.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await context.Connection.QuerySingleAsync<T>(command);
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCommandContextAsync(transaction);
        using var disposableConnection = context.ShouldDisposeConnection ? context.Connection : null;
        var command = new CommandDefinition(sql, param, context.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await context.Connection.ExecuteAsync(command);
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        var context = await GetCommandContextAsync(transaction);
        using var disposableConnection = context.ShouldDisposeConnection ? context.Connection : null;
        var command = new CommandDefinition(sql, param, context.Transaction, commandTimeout, commandType, cancellationToken: cancellationToken);
        return await context.Connection.ExecuteScalarAsync<T>(command);
    }

    private async Task<CommandContext> GetCommandContextAsync(IDbTransaction? transaction)
    {
        var activeTransaction = transaction ?? _unitOfWork.Transaction;
        if (activeTransaction is not null)
        {
            return new CommandContext(_unitOfWork.Connection, activeTransaction, false);
        }

        var connection = await OpenConnectionAsync();
        return new CommandContext(connection, null, true);
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

    private sealed record CommandContext(
        IDbConnection Connection,
        IDbTransaction? Transaction,
        bool ShouldDisposeConnection);
}
