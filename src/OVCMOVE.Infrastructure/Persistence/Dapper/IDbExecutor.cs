namespace OVCMOVE.Infrastructure.Persistence.Dapper;

public interface IDbExecutor
{
    Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default);

    Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<TFirst> First, IReadOnlyCollection<TSecond> Second)> QueryMultipleAsync<TFirst, TSecond>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default);

    Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default);

    Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default);
}
