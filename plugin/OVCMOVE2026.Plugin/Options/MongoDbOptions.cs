namespace OVCMOVE2026.Plugin.Options;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "ovcmove";
    public string CollectionName { get; init; } = "race_cards";
}
