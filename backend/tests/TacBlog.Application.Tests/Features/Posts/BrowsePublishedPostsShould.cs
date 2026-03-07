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
    public async Task return_published_posts_from_repository()
    {
        var post = BlogPost.Create(new Title("Published Post"), new PostContent("Content"), new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        post.Publish(new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        _repository.FindPublishedAsync(Arg.Any<CancellationToken>())
            .Returns([post]);

        var result = await _useCase.ExecuteAsync();

        result.Posts.Should().HaveCount(1);
        result.Posts[0].Title.ToString().Should().Be("Published Post");
    }

    [Fact]
    public async Task return_empty_list_when_no_published_posts_exist()
    {
        _repository.FindPublishedAsync(Arg.Any<CancellationToken>())
            .Returns(new List<BlogPost>());

        var result = await _useCase.ExecuteAsync();

        result.Posts.Should().BeEmpty();
    }
}
