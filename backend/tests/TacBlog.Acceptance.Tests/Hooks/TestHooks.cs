using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using TacBlog.Acceptance.Tests.Support;
using TacBlog.Application.Features.Auth;

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
        await _factory.EnsureMigratedAsync();

        var loginHandler = _factory.Services.GetRequiredService<LoginHandler>();
        loginHandler.ResetFailedAttempts();

        var stubImageStorage = _factory.Services.GetRequiredService<StubImageStorage>();
        stubImageStorage.ShouldFail = false;

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TacBlogDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            DO $$
            BEGIN
                IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'likes') THEN
                    DELETE FROM likes;
                END IF;
                IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'post_tags') THEN
                    DELETE FROM post_tags;
                END IF;
                DELETE FROM blog_posts;
                IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'tags') THEN
                    DELETE FROM tags;
                END IF;
            END $$;
            """);
    }
}
