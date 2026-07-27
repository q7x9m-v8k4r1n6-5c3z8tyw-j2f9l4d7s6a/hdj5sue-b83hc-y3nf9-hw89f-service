namespace OVCMOVE.Infrastructure.Options;

public class DbConfigOptions
{
    public const string SectionName = "DbConfig";
    public SqlServerOptions SqlServer { get; init; } = new();

    public class SqlServerOptions
    {
        public string ConnectionString { get; init; } = string.Empty;
    }
}
