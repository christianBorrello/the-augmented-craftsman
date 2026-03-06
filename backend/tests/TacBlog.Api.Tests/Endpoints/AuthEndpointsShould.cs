using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TacBlog.Api.Endpoints;
using TacBlog.Application.Ports.Driven;
using TacBlog.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace TacBlog.Api.Tests.Endpoints;

public sealed class AuthEndpointsShould : IAsyncLifetime
{
    private const string JwtSecret = "test-jwt-secret-key-minimum-32-characters-long!";
    private const string JwtIssuer = "TacBlog-Test";
    private const string AdminEmail = "admin@test.com";
    private const string AdminPassword = "test-admin-password";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tacblog_auth_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    var passwordHasher = new Infrastructure.Identity.AspNetPasswordHasher();
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AdminCredentials:Email"] = AdminEmail,
                        ["AdminCredentials:HashedPassword"] = passwordHasher.Hash(AdminPassword),
                        ["Jwt:Secret"] = JwtSecret,
                        ["Jwt:Issuer"] = JwtIssuer,
                        ["Jwt:ExpiryInMinutes"] = "60"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TacBlogDbContext>));

                    if (descriptor is not null)
                        services.Remove(descriptor);

                    services.AddDbContext<TacBlogDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });

                builder.UseEnvironment("Testing");
            });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TacBlogDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task reject_unauthenticated_post_with_401()
    {
        var response = await _client.PostAsJsonAsync("/api/posts",
            new { Title = "Test Post", Content = "Some content" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task accept_authenticated_post_with_201()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AdminEmail, AdminPassword));

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);

        var response = await _client.PostAsJsonAsync("/api/posts",
            new { Title = "Authenticated Post", Content = "Content from authenticated user" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("/api/posts")]
    [InlineData("/api/posts/any-slug")]
    public async Task allow_unauthenticated_get_requests(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
