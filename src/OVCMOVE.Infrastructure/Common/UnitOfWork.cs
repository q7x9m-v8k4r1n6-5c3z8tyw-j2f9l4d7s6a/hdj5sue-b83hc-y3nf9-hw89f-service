using System.Data;
using System.Data.Common;
using OVCMOVE.Application.Abstractions;
using OVCMOVE.Infrastructure.Persistence.SqlServer;

namespace OVCMOVE.Infrastructure.Common;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ISqlServerFactory _sqlServerFactory;
    private IDbConnection? _connection;
    private bool _disposed;

    public UnitOfWork(ISqlServerFactory sqlServerFactory)
    {
        _sqlServerFactory = sqlServerFactory;
    }

    internal IDbTransaction? Transaction { get; private set; }

    /// <summary>Starts the transaction shared by all repositories in this scope.</summary>
    public async Task BeginAsync(
        CancellationToken cancellationToken = default)
    {
        if (Transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active in this scope.");
        }

        _connection = _sqlServerFactory.CreateConnection();
        try
        {
            await EnsureConnectionOpenAsync(
                _connection,
                cancellationToken);
            Transaction = _connection is DbConnection dbConnection
                ? await dbConnection.BeginTransactionAsync(cancellationToken)
                : _connection.BeginTransaction();
        }
        catch
        {
            ReleaseConnection();
            throw;
        }
    }

    /// <summary>Commits and releases the active transaction.</summary>
    public async Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        if (Transaction is null)
        {
            throw new InvalidOperationException(
                "No active transaction to commit.");
        }

        if (Transaction is DbTransaction dbTransaction)
        {
            await dbTransaction.CommitAsync(cancellationToken);
            await dbTransaction.DisposeAsync();
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            Transaction.Commit();
            Transaction.Dispose();
        }

        Transaction = null;
        ReleaseConnection();
    }

    /// <summary>Rolls back and releases the active transaction, if present.</summary>
    public async Task RollbackAsync(
        CancellationToken cancellationToken = default)
    {
        if (Transaction is null)
        {
            return;
        }

        try
        {
            if (Transaction is DbTransaction dbTransaction)
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                await dbTransaction.DisposeAsync();
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                Transaction.Rollback();
                Transaction.Dispose();
            }
        }
        finally
        {
            Transaction = null;
            ReleaseConnection();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Transaction?.Dispose();
        ReleaseConnection();
        _disposed = true;
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

    private void ReleaseConnection()
    {
        if (_connection is null)
        {
            return;
        }

        if (_connection.State != ConnectionState.Closed)
        {
            _connection.Close();
        }

        _connection.Dispose();
        _connection = null;
    }
}
