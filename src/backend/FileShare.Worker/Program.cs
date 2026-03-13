using FileShare.Application;
using FileShare.Infrastructure;
using FileShare.Infrastructure.Persistence;
using FileShare.Worker.Configuration;
using FileShare.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<CleanupOptions>(builder.Configuration.GetSection(CleanupOptions.SectionName));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ExpiredFileCleanupBackgroundService>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FileShareDbContext>();
    dbContext.Database.EnsureCreated();
}

host.Run();
