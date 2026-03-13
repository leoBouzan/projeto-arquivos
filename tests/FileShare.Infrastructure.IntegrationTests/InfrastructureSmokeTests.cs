namespace FileShare.Infrastructure.IntegrationTests;

public sealed class InfrastructureSmokeTests
{
    [Fact]
    public void InfrastructureAssembly_ShouldLoad()
    {
        Assert.NotNull(typeof(FileShare.Infrastructure.DependencyInjection).Assembly);
    }
}
