using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using TacBlog.Acceptance.Tests.Support;

namespace TacBlog.Acceptance.Tests.Hooks;

[Binding]
public sealed class TestHooks
{
    private readonly TacBlogWebApplicationFactory _factory;

    public TestHooks(TacBlogWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [BeforeScenario]
    public async Task CleanDatabase()
    {
        // Delete all data between scenarios to ensure test isolation.
        // FK constraint order: post_tags → (blog_posts, tags).
        // post_tags references both blog_posts and tags, so it must be deleted first.
        // blog_posts and tags have no FK between them, so order doesn't matter after post_tags.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TacBlogDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM post_tags;
            DELETE FROM blog_posts;
            DELETE FROM tags;
            """);
    }
}
