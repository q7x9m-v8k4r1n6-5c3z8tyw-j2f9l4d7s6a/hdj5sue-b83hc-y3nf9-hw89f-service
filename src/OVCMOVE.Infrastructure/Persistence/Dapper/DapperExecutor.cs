using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using OVCMOVE.Application.Common;
using OVCMOVE.Infrastructure.Common;
using OVCMOVE.Infrastructure.Persistence.SqlServer;

namespace OVCMOVE.Infrastructure.Persistence.Dapper;

public class DapperExecutor : IDbExecutor
{
    private readonly ISqlServerFactory _sqlServerFactory;
    private readonly UnitOfWork _unitOfWork;

    private readonly record struct ConnectionScope(IDbConnection Connection, IDbTransaction? Transaction, bool DisposeConnection);

    public DapperExecutor(
        ISqlServerFactory sqlServerFactory,
        UnitOfWork unitOfWork)
    {
        _sqlServerFactory = sqlServerFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(cancellationToken);

        try
        {
            var command = new CommandDefinition(
                sql,
                param,
                scope.Transaction,
                cancellationToken: cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(cancellationToken);

        try
        {
            var command = new CommandDefinition(
                sql,
                param,
                scope.Transaction,
                cancellationToken: cancellationToken);
            return await scope.Connection.QueryFirstOrDefaultAsync<T>(command);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(cancellationToken);

        try
        {
            var command = new CommandDefinition(
                sql,
                param,
                scope.Transaction,
                cancellationToken: cancellationToken);
            return await scope.Connection.ExecuteAsync(command);
        }
        catch (SqlException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            throw new ApplicationConflictException(
                "Dữ liệu đã tồn tại hoặc đã được gán trước đó.",
                exception);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await OpenConnectionAsync(cancellationToken);

        try
        {
            var command = new CommandDefinition(
                sql,
                param,
                scope.Transaction,
                cancellationToken: cancellationToken);
            return await scope.Connection.ExecuteScalarAsync<T>(command);
        }
        catch (SqlException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            throw new ApplicationConflictException(
                "Dữ liệu đã tồn tại hoặc đã được gán trước đó.",
                exception);
        }
        finally
        {
            DisposeConnection(scope);
        }
    }

    private async Task<ConnectionScope> OpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        var effectiveTransaction = _unitOfWork.Transaction;
        var transactionConnection = effectiveTransaction?.Connection;

        if (transactionConnection is not null)
        {
            await EnsureConnectionOpenAsync(
                transactionConnection,
                cancellationToken);
            return new ConnectionScope(transactionConnection, effectiveTransaction, false);
        }

        var connection = _sqlServerFactory.CreateConnection();
        try
        {
            await EnsureConnectionOpenAsync(connection, cancellationToken);
            return new ConnectionScope(connection, null, true);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static async Task EnsureConnectionOpenAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.State == ConnectionState.Open)
        {
            return;
        }

        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
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

    /// <summary>Recognizes SQL Server duplicate-key errors raised by unique indexes.</summary>
    private static bool IsUniqueConstraintViolation(SqlException exception) =>
        exception.Number is 2601 or 2627;
}
