using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FileShare.API.IntegrationTests;

public sealed class FilesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FilesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Availability_ShouldReturnNotFound_WhenTokenDoesNotExist()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/files/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/availability");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Availability_ShouldReturnNotFound_WhenTokenFormatIsInvalid()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/files/invalid-token/availability");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Availability_ShouldReturnTooManyRequests_WhenPublicReadRateLimitIsExceeded()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        const string path = "/api/files/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/availability";

        HttpResponseMessage? lastResponse = null;
        for (var attempt = 0; attempt < 61; attempt++)
        {
            lastResponse = await client.GetAsync(path);
        }

        Assert.NotNull(lastResponse);
        var body = await lastResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        Assert.Contains("rate_limit.exceeded", body);
    }
}
