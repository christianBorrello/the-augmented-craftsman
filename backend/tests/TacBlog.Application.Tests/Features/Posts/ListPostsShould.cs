using FluentAssertions;
using NSubstitute;
using TacBlog.Application.Features.Posts;
using TacBlog.Application.Ports.Driven;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Application.Tests.Features.Posts;

public class ListPostsShould
{
    private readonly IBlogPostRepository _repository = Substitute.For<IBlogPostRepository>();
    private readonly ListPosts _useCase;

    public ListPostsShould()
    {
        _useCase = new ListPosts(_repository);
    }

    [Fact]
    public async Task return_all_posts_sorted_by_created_date_descending()
    {
        var oldest = BlogPost.Create(new Title("Oldest Post"), new PostContent("Content"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var middle = BlogPost.Create(new Title("Middle Post"), new PostContent("Content"), new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var newest = BlogPost.Create(new Title("Newest Post"), new PostContent("Content"), new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns([oldest, middle, newest]);

        var result = await _useCase.ExecuteAsync();

        result.Posts.Should().HaveCount(3);
        result.Posts[0].Title.ToString().Should().Be("Newest Post");
        result.Posts[1].Title.ToString().Should().Be("Middle Post");
        result.Posts[2].Title.ToString().Should().Be("Oldest Post");
    }

    [Fact]
    public async Task return_empty_list_when_no_posts_exist()
    {
        _repository.FindAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<BlogPost>());

        var result = await _useCase.ExecuteAsync();

        result.Posts.Should().BeEmpty();
    }
}
