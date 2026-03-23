using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TacBlog.Application.Ports.Driven;
using TacBlog.Infrastructure.Storage;
using Testcontainers.PostgreSql;
using Xunit;

namespace TacBlog.Acceptance.Tests.Support;

public sealed class TacBlogWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestAdminApiKey = "test-admin-api-key";

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
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = TestAdminApiKey,
                ["OAuth:GitHub:ClientId"] = "test-github-client-id",
                ["OAuth:GitHub:ClientSecret"] = "test-github-client-secret"
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

            var imageStorageDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IImageStorage));
            if (imageStorageDescriptor is not null)
                services.Remove(imageStorageDescriptor);

            var settingsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ImageKitSettings));
            if (settingsDescriptor is not null)
                services.Remove(settingsDescriptor);

            services.AddSingleton<StubImageStorage>();
            services.AddSingleton<IImageStorage>(sp => sp.GetRequiredService<StubImageStorage>());

            var oauthDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IOAuthClient));
            if (oauthDescriptor is not null)
                services.Remove(oauthDescriptor);

            services.AddSingleton<StubOAuthClient>();
            services.AddSingleton<IOAuthClient>(sp => sp.GetRequiredService<StubOAuthClient>());
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
