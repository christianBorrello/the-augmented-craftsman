using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class PublishScheduledPostsShould
{
    private static readonly DateTime FixedNow = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly PublishScheduledPosts _useCase;

    public PublishScheduledPostsShould()
    {
        _clock.UtcNow.Returns(FixedNow);
        _useCase = new PublishScheduledPosts(_repository, _clock);
    }

    [Fact]
    public async Task publish_only_posts_that_are_due()
    {
        var duePost1 = CreateScheduledPost("First Post", FixedNow.AddHours(-2));
        var duePost2 = CreateScheduledPost("Second Post", FixedNow.AddMinutes(-30));

        _repository.FindScheduledDueAsync(FixedNow, Arg.Any<CancellationToken>())
            .Returns([duePost1, duePost2]);

        var result = await _useCase.ExecuteAsync();

        result.PublishedSlugs.Should().HaveCount(2);
        duePost1.Status.Should().Be(PostStatus.Published);
        duePost2.Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public async Task set_published_at_to_the_scheduled_time()
    {
        var scheduledTime = FixedNow.AddHours(-1);
        var post = CreateScheduledPost("Timed Post", scheduledTime);

        _repository.FindScheduledDueAsync(FixedNow, Arg.Any<CancellationToken>())
            .Returns([post]);

        await _useCase.ExecuteAsync();

        post.PublishedAt.Should().Be(scheduledTime);
        post.ScheduledAt.Should().BeNull();
    }

    [Fact]
    public async Task return_published_slugs()
    {
        var post = CreateScheduledPost("My Slug Post", FixedNow.AddHours(-1));

        _repository.FindScheduledDueAsync(FixedNow, Arg.Any<CancellationToken>())
            .Returns([post]);

        var result = await _useCase.ExecuteAsync();

        result.PublishedSlugs.Should().Contain(post.Slug.Value);
    }

    [Fact]
    public async Task save_each_published_post()
    {
        var post1 = CreateScheduledPost("Post A", FixedNow.AddHours(-2));
        var post2 = CreateScheduledPost("Post B", FixedNow.AddHours(-1));

        _repository.FindScheduledDueAsync(FixedNow, Arg.Any<CancellationToken>())
            .Returns([post1, post2]);

        await _useCase.ExecuteAsync();

        await _repository.Received(2).SaveAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task return_empty_when_no_posts_are_due()
    {
        _repository.FindScheduledDueAsync(FixedNow, Arg.Any<CancellationToken>())
            .Returns(new List<BlogPost>());

        var result = await _useCase.ExecuteAsync();

        result.PublishedSlugs.Should().BeEmpty();
        await _repository.DidNotReceive().SaveAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
    }

    private static BlogPost CreateScheduledPost(string title, DateTime scheduledAt)
    {
        var createdAt = scheduledAt.AddDays(-1);
        var post = BlogPost.Create(new Title(title), new PostContent("Content"), createdAt);
        post.Schedule(scheduledAt, createdAt);
        return post;
    }
}
