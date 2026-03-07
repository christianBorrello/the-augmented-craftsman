using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using TacBlog.Infrastructure;
using TacBlog.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace TacBlog.Api.Tests.Persistence;

public sealed class EfTagRepositoryShould : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tacblog_tag_repo_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private TacBlogDbContext _context = null!;
    private EfTagRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<TacBlogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new TacBlogDbContext(options);
        await _context.Database.MigrateAsync();
        _repository = new EfTagRepository(_context);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task save_and_find_tag_by_slug()
    {
        var tag = Tag.Create(new TagName("Clean Code"));

        await _repository.SaveAsync(tag, CancellationToken.None);

        var found = await _repository.FindBySlugAsync(new Slug("clean-code"), CancellationToken.None);

        found.Should().NotBeNull();
        found!.Name.Should().Be(new TagName("Clean Code"));
        found.Slug.Should().Be(new Slug("clean-code"));
    }

    [Fact]
    public async Task save_and_find_tag_by_id()
    {
        var tag = Tag.Create(new TagName("TDD"));

        await _repository.SaveAsync(tag, CancellationToken.None);

        var found = await _repository.FindByIdAsync(tag.Id, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Id.Should().Be(tag.Id);
        found.Name.Should().Be(new TagName("TDD"));
    }

    [Fact]
    public async Task return_null_when_slug_not_found()
    {
        var found = await _repository.FindBySlugAsync(new Slug("nonexistent"), CancellationToken.None);

        found.Should().BeNull();
    }

    [Fact]
    public async Task report_existence_by_slug()
    {
        var tag = Tag.Create(new TagName("SOLID"));
        await _repository.SaveAsync(tag, CancellationToken.None);

        var exists = await _repository.ExistsBySlugAsync(new Slug("solid"), CancellationToken.None);
        var notExists = await _repository.ExistsBySlugAsync(new Slug("missing"), CancellationToken.None);

        exists.Should().BeTrue();
        notExists.Should().BeFalse();
    }

    [Fact]
    public async Task delete_tag_and_remove_from_post_tags()
    {
        var tag = Tag.Create(new TagName("Refactoring"));
        await _repository.SaveAsync(tag, CancellationToken.None);

        var post = BlogPost.Create(new Title("Test Post"), new PostContent("Content"), DateTime.UtcNow);
        post.AddTag(tag);
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        DetachAll();

        await _repository.DeleteAsync(tag.Id, CancellationToken.None);

        var found = await _repository.FindBySlugAsync(new Slug("refactoring"), CancellationToken.None);
        found.Should().BeNull();

        var postReloaded = await _context.Posts.Include(p => p.Tags)
            .SingleAsync(p => p.Id == post.Id);
        postReloaded.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task return_all_tags_with_post_counts()
    {
        var tdd = Tag.Create(new TagName("TDD"));
        var ddd = Tag.Create(new TagName("DDD"));
        var unused = Tag.Create(new TagName("Unused"));
        await _repository.SaveAsync(tdd, CancellationToken.None);
        await _repository.SaveAsync(ddd, CancellationToken.None);
        await _repository.SaveAsync(unused, CancellationToken.None);

        var post1 = BlogPost.Create(new Title("Post One"), new PostContent("Content"), DateTime.UtcNow);
        post1.AddTag(tdd);
        post1.AddTag(ddd);
        _context.Posts.Add(post1);

        var post2 = BlogPost.Create(new Title("Post Two"), new PostContent("Content"), DateTime.UtcNow);
        post2.AddTag(tdd);
        _context.Posts.Add(post2);

        await _context.SaveChangesAsync();

        DetachAll();

        var results = await _repository.GetAllWithPostCountsAsync(CancellationToken.None);

        results.Should().HaveCount(3);

        var tddResult = results.Single(r => r.Tag.Slug == new Slug("tdd"));
        tddResult.PostCount.Should().Be(2);

        var dddResult = results.Single(r => r.Tag.Slug == new Slug("ddd"));
        dddResult.PostCount.Should().Be(1);

        var unusedResult = results.Single(r => r.Tag.Slug == new Slug("unused"));
        unusedResult.PostCount.Should().Be(0);
    }

    [Fact]
    public async Task update_existing_tag_on_save()
    {
        var tag = Tag.Create(new TagName("Old Name"));
        await _repository.SaveAsync(tag, CancellationToken.None);

        tag.Rename(new TagName("New Name"));
        await _repository.SaveAsync(tag, CancellationToken.None);

        DetachAll();

        var found = await _repository.FindByIdAsync(tag.Id, CancellationToken.None);
        found!.Name.Should().Be(new TagName("New Name"));
        found.Slug.Should().Be(new Slug("new-name"));
    }

    private void DetachAll()
    {
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
            entry.State = EntityState.Detached;
    }
}
