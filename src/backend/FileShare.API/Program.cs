using System.Threading.RateLimiting;
using FileShare.API.Configuration;
using FileShare.Application;
using FileShare.API.Middleware;
using FileShare.Infrastructure;
using FileShare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors("frontend");
app.UseRateLimiter();
app.MapControllers();
app.Run();

public partial class Program;
