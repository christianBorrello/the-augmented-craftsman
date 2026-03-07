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
using TacBlog.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace TacBlog.Api.Tests.Endpoints;

public sealed class PostEndpointsShould : IAsyncLifetime
{
    private const string JwtSecret = "test-jwt-secret-key-minimum-32-characters-long!";
    private const string JwtIssuer = "TacBlog-Test";
    private const string AdminEmail = "admin@test.com";
    private const string AdminPassword = "test-admin-password";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tacblog_post_test")
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

        await AuthenticateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AdminEmail, AdminPassword));

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);
    }

    private async Task<PostResponse> CreatePostViaApiAsync(
        string title = "Test Post",
        string content = "Test content",
        string[]? tags = null)
    {
        var request = new CreatePostRequest(title, content, tags);
        var response = await _client.PostAsJsonAsync("/api/posts", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PostResponse>())!;
    }

    [Fact]
    public async Task update_post_and_return_200()
    {
        var created = await CreatePostViaApiAsync("Original Title", "Original content");

        var response = await _client.PutAsJsonAsync($"/api/posts/{created.Id}",
            new EditPostRequest("Updated Title", "Updated content"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PostResponse>();
        updated!.Title.Should().Be("Updated Title");
        updated.Content.Should().Be("Updated content");
    }

    [Fact]
    public async Task return_404_when_updating_nonexistent_post()
    {
        var response = await _client.PutAsJsonAsync($"/api/posts/{Guid.NewGuid()}",
            new EditPostRequest("Title", "Content"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task return_400_when_updating_with_empty_title()
    {
        var created = await CreatePostViaApiAsync();

        var response = await _client.PutAsJsonAsync($"/api/posts/{created.Id}",
            new EditPostRequest("", "Content"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task delete_post_and_return_204()
    {
        var created = await CreatePostViaApiAsync();

        var response = await _client.DeleteAsync($"/api/posts/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task return_404_when_deleting_nonexistent_post()
    {
        var response = await _client.DeleteAsync($"/api/posts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task publish_post_and_return_200()
    {
        var created = await CreatePostViaApiAsync();

        var response = await _client.PostAsync($"/api/posts/{created.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await response.Content.ReadFromJsonAsync<PostResponse>();
        published!.Status.Should().Be("Published");
        published.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task return_404_when_publishing_nonexistent_post()
    {
        var response = await _client.PostAsync($"/api/posts/{Guid.NewGuid()}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task return_409_when_publishing_already_published_post()
    {
        var created = await CreatePostViaApiAsync();
        await _client.PostAsync($"/api/posts/{created.Id}/publish", null);

        var response = await _client.PostAsync($"/api/posts/{created.Id}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task list_all_posts_for_admin()
    {
        await CreatePostViaApiAsync("First Post", "Content one");
        await CreatePostViaApiAsync("Second Post", "Content two");

        var response = await _client.GetAsync("/api/admin/posts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<PostResponse[]>();
        posts!.Length.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task require_auth_for_admin_list()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/admin/posts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unauthenticatedClient.Dispose();
    }

    [Fact]
    public async Task create_post_with_tags()
    {
        var created = await CreatePostViaApiAsync("Tagged Post", "Content", ["TDD", "Clean Code"]);

        created.Tags.Should().NotBeNull();
        created.Tags.Should().Contain("TDD");
        created.Tags.Should().Contain("Clean Code");
    }

    [Fact]
    public async Task update_post_with_tags()
    {
        var created = await CreatePostViaApiAsync("Post", "Content");

        var response = await _client.PutAsJsonAsync($"/api/posts/{created.Id}",
            new EditPostRequest("Updated", "Content", ["DDD", "SOLID"]));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PostResponse>();
        updated!.Tags.Should().Contain("DDD");
        updated.Tags.Should().Contain("SOLID");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task require_auth_for_write_endpoints(string method)
    {
        var unauthenticatedClient = _factory.CreateClient();
        var id = Guid.NewGuid();

        var response = method switch
        {
            "PUT" => await unauthenticatedClient.PutAsJsonAsync($"/api/posts/{id}",
                new EditPostRequest("Title", "Content")),
            "DELETE" => await unauthenticatedClient.DeleteAsync($"/api/posts/{id}"),
            _ => throw new ArgumentException($"Unexpected method: {method}")
        };

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unauthenticatedClient.Dispose();
    }

    [Fact]
    public async Task require_auth_for_publish()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var response = await unauthenticatedClient.PostAsync(
            $"/api/posts/{Guid.NewGuid()}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unauthenticatedClient.Dispose();
    }
}
