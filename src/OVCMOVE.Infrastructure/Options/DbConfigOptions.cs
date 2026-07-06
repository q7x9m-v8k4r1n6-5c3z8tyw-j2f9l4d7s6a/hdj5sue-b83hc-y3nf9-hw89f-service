namespace OVCMOVE.Infrastructure.Options;

public class DbConfigOptions
{
    public const string SectionName = "DbConfig";
    public SQLServerOptions SQLServer { get; init; } 

    public class SQLServerOptions
    {
        public string ConnectionString { get; init; } = string.Empty;
    }

}
