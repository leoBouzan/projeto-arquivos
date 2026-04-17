using FileShare.Application.Abstractions.Messaging;
using FileShare.Application.Abstractions.Persistence;
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

namespace FileShare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        var persistenceOptions = configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>() ?? new PersistenceOptions();

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

        return services;
    }
}
