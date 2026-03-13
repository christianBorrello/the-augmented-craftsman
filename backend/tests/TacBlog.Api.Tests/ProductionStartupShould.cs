using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TacBlog.Api.Tests;

public class StartupShould
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task build_di_container_without_exceptions(string environment)
    {
        // Arrange
        await using var factory = new SmokeTestWebApplicationFactory(environment);

        // Act — CreateClient() triggers full Program.cs startup, including DI container build
        var act = () => Task.FromResult(factory.CreateClient());

        // Assert — catches InvalidOperationException from any unregistered DI dependency,
        // in both dev and prod branches of Program.cs (e.g. the HttpClient/ProductionOAuthClient
        // bug that caused 503 on all production endpoints).
        await act.Should().NotThrowAsync();
    }
}

/// <summary>
/// Bootstraps the API with fake secrets and no real DB or external calls.
/// Supports both Development and Production environments to catch DI composition
/// root failures in either branch of Program.cs.
/// </summary>
internal sealed class SmokeTestWebApplicationFactory(string environment) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);

        // Suppress DB migration — the test exercises DI resolution, not schema correctness.
        builder.UseSetting("Database:RunMigrationsAtStartup", "false");

        // DB connection string must always be non-null (read during builder phase).
        // The connection is never opened because migration is suppressed above.
        builder.UseSetting("ConnectionStrings:DefaultConnection",
            "Host=localhost;Database=fake;Username=fake;Password=fake");

        // Production-only secrets (Program.cs throws if missing when !IsDevelopment()).
        // Providing them in both environments is safe — dev just ignores them.
        builder.UseSetting("Jwt:Secret", "fake-jwt-secret-for-di-smoke-test-minimum-length-32-chars");
        builder.UseSetting("AdminCredentials:Email", "admin@smoke-test.invalid");
        builder.UseSetting("AdminCredentials:HashedPassword",
            "AQAAAAIAAYagAAAAEFakeHashedPasswordForSmokeTestOnly==");
        builder.UseSetting("OAuth:GitHub:ClientId", "fake-github-client-id");
        builder.UseSetting("OAuth:GitHub:ClientSecret", "fake-github-client-secret");
    }
}
