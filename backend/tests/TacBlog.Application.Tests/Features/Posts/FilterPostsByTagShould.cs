using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class FilterPostsByTagShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly FilterPostsByTag _useCase;

    public FilterPostsByTagShould()
    {
        _useCase = new FilterPostsByTag(_repository);
    }

    [Fact]
    public async Task return_published_posts_that_have_the_tag()
    {
        var tag = Tag.Create(new TagName("csharp"));
        _repository.FindTagBySlugAsync(Arg.Is<Slug>(s => s.Value == "csharp"), Arg.Any<CancellationToken>())
            .Returns(tag);

        var matchingPost = BlogPost.Create(new Title("C# Tips"), new PostContent("Content"), FixedNow);
        matchingPost.AddTag(tag);
        matchingPost.Publish(FixedNow);

        _repository.FindPublishedByTagSlugAsync(Arg.Is<Slug>(s => s.Value == "csharp"), Arg.Any<CancellationToken>())
            .Returns([matchingPost]);

        var result = await _useCase.ExecuteAsync("csharp");

        result.IsSuccess.Should().BeTrue();
        result.IsNotFound.Should().BeFalse();
        result.Posts.Should().HaveCount(1);
        result.Posts![0].Title.ToString().Should().Be("C# Tips");
    }

    [Fact]
    public async Task return_not_found_when_tag_slug_does_not_exist()
    {
        _repository.FindTagBySlugAsync(Arg.Is<Slug>(s => s.Value == "nonexistent"), Arg.Any<CancellationToken>())
            .Returns((Tag?)null);

        var result = await _useCase.ExecuteAsync("nonexistent");

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Posts.Should().BeNull();
    }

    [Fact]
    public async Task return_empty_list_when_tag_exists_but_has_no_published_posts()
    {
        var tag = Tag.Create(new TagName("empty-tag"));
        _repository.FindTagBySlugAsync(Arg.Is<Slug>(s => s.Value == "empty-tag"), Arg.Any<CancellationToken>())
            .Returns(tag);

        _repository.FindPublishedByTagSlugAsync(Arg.Is<Slug>(s => s.Value == "empty-tag"), Arg.Any<CancellationToken>())
            .Returns(new List<BlogPost>());

        var result = await _useCase.ExecuteAsync("empty-tag");

        result.IsSuccess.Should().BeTrue();
        result.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task return_not_found_for_invalid_slug()
    {
        var result = await _useCase.ExecuteAsync("");

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Posts.Should().BeNull();
    }
}
