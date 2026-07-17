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
        CommandType? commandType = null)
    {
        using var connection = await OpenConnectionAsync();

        return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        using var connection = await OpenConnectionAsync();

        return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        using var connection = await OpenConnectionAsync();

        return await connection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        using var connection = await OpenConnectionAsync();

        return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        using var connection = await OpenConnectionAsync();

        return await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
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
