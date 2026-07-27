namespace OVCMOVE.Infrastructure.Common;

internal static class PersistenceWriteGuard
{
    /// <summary>Fails fast when an expected single-row insert writes nothing.</summary>
    internal static void EnsureInserted(int affectedRows, string entityName)
    {
        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"Expected to insert one {entityName}, but {affectedRows} rows were affected.");
        }
    }
}
