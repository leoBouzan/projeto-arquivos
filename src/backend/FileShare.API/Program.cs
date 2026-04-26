using System.Threading.RateLimiting;
using DotNetEnv;
using FileShare.API.Configuration;
using FileShare.Application;
using FileShare.API.Middleware;
using FileShare.Infrastructure;
using FileShare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

// Load environment variables from a `.env` file at the repository root.
// This file is git-ignored — sensitive keys (e.g. VIRUSTOTAL_API_KEY) live there.
// If the file is absent, no error is thrown — the app falls back to its default behavior
// (e.g. an emulated malware scanner when no VirusTotal key is provided).
foreach (var candidate in new[] { ".env", "../.env", "../../.env", "../../../.env", "../../../../.env" })
{
    var fullPath = Path.GetFullPath(candidate);
    if (File.Exists(fullPath))
    {
        Env.Load(fullPath);
        break;
    }
}

var builder = WebApplication.CreateBuilder(args);
var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FileShare API",
        Version = "v1",
        Description =
            "API do FileShare - sistema de compartilhamento temporario de arquivos com expiracao, " +
            "limite de downloads, prova de transferencia (hash + assinatura) e escaneamento antivirus " +
            "via VirusTotal (com fallback emulado quando a chave nao esta configurada).",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Repositorio",
            Url = new Uri("https://github.com/LCGant/projeto-arquivos")
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = securityOptions.MaxUploadSizeBytes;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        if (securityOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(securityOptions.AllowedOrigins);
        }

        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title = "rate_limit.exceeded",
            detail = "Too many requests. Please wait and try again.",
            status = StatusCodes.Status429TooManyRequests
        }, cancellationToken);
    };
    var globalIpLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            RequestPartitionKeyResolver.ResolveIpKey(httpContext),
            _ => RateLimiterOptionsFactory.Create(securityOptions.RateLimiting.GlobalIp)));
    var operationLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RequestRateLimitPartitionResolver.ResolveOperationPartition(httpContext, securityOptions));
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(globalIpLimiter, operationLimiter);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FileShareDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseMiddleware<UnhandledExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<DeviceFingerprintMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FileShare API v1");
    options.DocumentTitle = "FileShare API - Swagger";
    options.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseCors("frontend");
app.UseRateLimiter();
app.MapControllers();
app.Run();

public partial class Program;
