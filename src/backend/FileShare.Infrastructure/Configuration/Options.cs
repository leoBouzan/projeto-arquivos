namespace FileShare.Infrastructure.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public string Provider { get; set; } = "Sqlite";

    public string DatabaseName { get; set; } = "FileShare";

    public string? ConnectionString { get; set; } = "Data Source=App_Data/fileshare.db";
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "App_Data/storage";
}
