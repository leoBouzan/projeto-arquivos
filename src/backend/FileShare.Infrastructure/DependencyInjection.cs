using FileShare.Application.Abstractions.Messaging;
using FileShare.Application.Abstractions.Persistence;
using FileShare.Application.Abstractions.Scanning;
using FileShare.Application.Abstractions.Security;
using FileShare.Application.Abstractions.Storage;
using FileShare.Application.Abstractions.Time;
using FileShare.Infrastructure.Configuration;
using FileShare.Infrastructure.Messaging;
using FileShare.Infrastructure.Paths;
using FileShare.Infrastructure.Persistence;
using FileShare.Infrastructure.Persistence.Repositories;
using FileShare.Infrastructure.Security;
using FileShare.Infrastructure.Storage.Local;
using FileShare.Infrastructure.Time;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileShare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<VirusTotalOptions>(configuration.GetSection(VirusTotalOptions.SectionName));

        var persistenceOptions = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>() ?? new PersistenceOptions();
        var virusTotalOptions = configuration.GetSection(VirusTotalOptions.SectionName).Get<VirusTotalOptions>() ?? new VirusTotalOptions();

        // Environment variable takes precedence over appsettings — the .env file is the canonical source for the API key.
        var envApiKey = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY");
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            virusTotalOptions.ApiKey = envApiKey;
            services.PostConfigure<VirusTotalOptions>(opts => opts.ApiKey = envApiKey);
        }

        services.AddDbContext<FileShareDbContext>(options =>
        {
            var provider = persistenceOptions.Provider.Trim().ToLowerInvariant();

            if (provider == "inmemory")
            {
                options.UseInMemoryDatabase(persistenceOptions.DatabaseName);
            }
            else if (provider == "sqlite")
            {
                var connectionString = persistenceOptions.ConnectionString ?? "Data Source=App_Data/fileshare.db";
                var builder = new SqliteConnectionStringBuilder(connectionString);

                if (!string.IsNullOrWhiteSpace(builder.DataSource))
                {
                    var databasePath = PathResolver.ResolveFromRepositoryRoot(builder.DataSource);

                    var databaseDirectory = Path.GetDirectoryName(databasePath);
                    if (!string.IsNullOrWhiteSpace(databaseDirectory))
                    {
                        Directory.CreateDirectory(databaseDirectory);
                    }

                    builder.DataSource = databasePath;
                }

                options.UseSqlite(builder.ConnectionString);
            }
            else
            {
                options.UseNpgsql(persistenceOptions.ConnectionString);
            }
        });

        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IEventBus, NoOpEventBus>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddMemoryCache();
        services.AddSingleton<IMalwareScanPolicy>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<VirusTotalOptions>>().Value;
            return new MalwareScanPolicy(opts.BlockOnMalicious);
        });

        if (!string.IsNullOrWhiteSpace(virusTotalOptions.ApiKey))
        {
            services.AddHttpClient<IMalwareScanner, VirusTotalMalwareScanner>(client =>
            {
                client.BaseAddress = new Uri("https://www.virustotal.com/api/v3/");
                client.DefaultRequestHeaders.Add("x-apikey", virusTotalOptions.ApiKey);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(virusTotalOptions.TimeoutSeconds);
            });
        }
        else
        {
            // No VIRUSTOTAL_API_KEY in `.env` — fall back to a deterministic emulated scanner.
            // Results are flagged with IsEmulated=true so the UI can warn the user that the scan is fake.
            services.AddSingleton<IMalwareScanner, EmulatedMalwareScanner>();
        }

        return services;
    }
}

internal sealed class MalwareScanPolicy : IMalwareScanPolicy
{
    public MalwareScanPolicy(bool blockOnMalicious)
    {
        BlockOnMalicious = blockOnMalicious;
    }

    public bool BlockOnMalicious { get; }
}
