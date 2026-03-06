using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace TacBlog.Acceptance.Tests.Support;

public sealed class TacBlogWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestAdminEmail = "christian.borrello@live.it";
    private const string TestAdminPassword = "valid-password";
    private const string TestJwtSecret = "test-jwt-secret-key-minimum-32-characters-long!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tacblog_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private bool _migrated;

    public string ConnectionString => _postgres.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var hashedPassword = new PasswordHasher<string>().HashPassword(string.Empty, TestAdminPassword);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminCredentials:Email"] = TestAdminEmail,
                ["AdminCredentials:HashedPassword"] = hashedPassword,
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = "TacBlog-Test",
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
    }

    public async Task StartContainerAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task EnsureMigratedAsync()
    {
        if (_migrated) return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TacBlogDbContext>();
        await db.Database.MigrateAsync();
        _migrated = true;
    }

    public async Task InitializeAsync()
    {
        await StartContainerAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
