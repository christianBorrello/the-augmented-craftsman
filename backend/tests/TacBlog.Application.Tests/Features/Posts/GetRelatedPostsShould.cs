using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class GetRelatedPostsShould
{
    private static readonly DateTime FixedNow = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly GetRelatedPosts _useCase;

    public GetRelatedPostsShould()
    {
        _useCase = new GetRelatedPosts(_repository);
    }

    [Fact]
    public async Task return_not_found_when_slug_does_not_exist()
    {
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns((BlogPost?)null);

        var result = await _useCase.ExecuteAsync("non-existent-slug");

        result.IsNotFound.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task return_empty_list_when_no_related_posts_exist()
    {
        var source = CreatePublishedPost("Source Post", FixedNow, "tdd");
        SetupSourcePost(source);
        var unrelated = CreatePublishedPost("Unrelated Post", FixedNow.AddDays(1), "docker");
        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([source, unrelated]);

        var result = await _useCase.ExecuteAsync("source-post");

        result.IsSuccess.Should().BeTrue();
        result.Posts.Should().BeEmpty();
    }

    [Fact]
    public async Task return_up_to_3_related_published_posts()
    {
        var sharedTag = Tag.Create(new TagName("tdd"));
        var source = CreatePublishedPostWithTags("Source Post", FixedNow, sharedTag);
        SetupSourcePost(source);

        var related1 = CreatePublishedPostWithTags("Related One", FixedNow.AddDays(1), sharedTag);
        var related2 = CreatePublishedPostWithTags("Related Two", FixedNow.AddDays(2), sharedTag);
        var related3 = CreatePublishedPostWithTags("Related Three", FixedNow.AddDays(3), sharedTag);
        var related4 = CreatePublishedPostWithTags("Related Four", FixedNow.AddDays(4), sharedTag);

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([source, related1, related2, related3, related4]);

        var result = await _useCase.ExecuteAsync("source-post");

        result.IsSuccess.Should().BeTrue();
        result.Posts.Should().HaveCount(3);
    }

    [Fact]
    public async Task rank_by_shared_tag_count_descending()
    {
        var tdd = Tag.Create(new TagName("tdd"));
        var ddd = Tag.Create(new TagName("ddd"));
        var clean = Tag.Create(new TagName("clean-code"));

        var source = CreatePublishedPostWithTags("Source Post", FixedNow, tdd, ddd, clean);
        SetupSourcePost(source);

        var oneShared = CreatePublishedPostWithTags("One Shared", FixedNow.AddDays(1), tdd);
        var twoShared = CreatePublishedPostWithTags("Two Shared", FixedNow.AddDays(2), tdd, ddd);
        var threeShared = CreatePublishedPostWithTags("Three Shared", FixedNow.AddDays(3), tdd, ddd, clean);

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([source, oneShared, twoShared, threeShared]);

        var result = await _useCase.ExecuteAsync("source-post");

        result.Posts![0].Title.ToString().Should().Be("Three Shared");
        result.Posts[1].Title.ToString().Should().Be("Two Shared");
        result.Posts[2].Title.ToString().Should().Be("One Shared");
    }

    [Fact]
    public async Task break_ties_by_published_at_newest_first()
    {
        var sharedTag = Tag.Create(new TagName("tdd"));
        var source = CreatePublishedPostWithTags("Source Post", FixedNow, sharedTag);
        SetupSourcePost(source);

        var older = CreatePublishedPostWithTags("Older Post", FixedNow.AddDays(1), sharedTag);
        var newer = CreatePublishedPostWithTags("Newer Post", FixedNow.AddDays(5), sharedTag);

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([source, older, newer]);

        var result = await _useCase.ExecuteAsync("source-post");

        result.Posts![0].Title.ToString().Should().Be("Newer Post");
        result.Posts[1].Title.ToString().Should().Be("Older Post");
    }

    [Fact]
    public async Task exclude_source_post_from_results()
    {
        var sharedTag = Tag.Create(new TagName("tdd"));
        var source = CreatePublishedPostWithTags("Source Post", FixedNow, sharedTag);
        SetupSourcePost(source);

        var related = CreatePublishedPostWithTags("Related Post", FixedNow.AddDays(1), sharedTag);

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([source, related]);

        var result = await _useCase.ExecuteAsync("source-post");

        result.Posts.Should().HaveCount(1);
        result.Posts![0].Title.ToString().Should().Be("Related Post");
    }

    private void SetupSourcePost(BlogPost post)
    {
        _repository.FindBySlugAsync(Arg.Any<Slug>(), Arg.Any<CancellationToken>())
            .Returns(post);
    }

    private static BlogPost CreatePublishedPost(string title, DateTime publishedAt, params string[] tagNames)
    {
        var post = BlogPost.Create(new Title(title), new PostContent("Content"), FixedNow);
        foreach (var tagName in tagNames)
            post.AddTag(Tag.Create(new TagName(tagName)));
        post.Publish(publishedAt);
        return post;
    }

    private static BlogPost CreatePublishedPostWithTags(string title, DateTime publishedAt, params Tag[] tags)
    {
        var post = BlogPost.Create(new Title(title), new PostContent("Content"), FixedNow);
        foreach (var tag in tags)
            post.AddTag(tag);
        post.Publish(publishedAt);
        return post;
    }
}
