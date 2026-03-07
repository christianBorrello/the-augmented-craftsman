using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class BrowsePublishedPostsShould
{
    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly BrowsePublishedPosts _useCase;

    public BrowsePublishedPostsShould()
    {
        _useCase = new BrowsePublishedPosts(_repository);
    }

    [Fact]
    public async Task return_only_published_posts_excluding_drafts()
    {
        var draft = BlogPost.Create(new Title("Draft Post"), new PostContent("Content"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var published = BlogPost.Create(new Title("Published Post"), new PostContent("Content"), new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        published.Publish(new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([draft, published]);

        var result = await _useCase.ExecuteAsync();

        result.Posts.Should().HaveCount(1);
        result.Posts[0].Title.ToString().Should().Be("Published Post");
    }

    [Fact]
    public async Task return_published_posts_sorted_by_published_at_descending()
    {
        var earlier = BlogPost.Create(new Title("Earlier Post"), new PostContent("Content"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        earlier.Publish(new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc));

        var later = BlogPost.Create(new Title("Later Post"), new PostContent("Content"), new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        later.Publish(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([earlier, later]);

        var result = await _useCase.ExecuteAsync();

        result.Posts.Should().HaveCount(2);
        result.Posts[0].Title.ToString().Should().Be("Later Post");
        result.Posts[1].Title.ToString().Should().Be("Earlier Post");
    }

    [Fact]
    public async Task return_empty_list_when_no_published_posts_exist()
    {
        var draft = BlogPost.Create(new Title("Draft Only"), new PostContent("Content"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([draft]);

        var result = await _useCase.ExecuteAsync();

        result.Posts.Should().BeEmpty();
    }
}
