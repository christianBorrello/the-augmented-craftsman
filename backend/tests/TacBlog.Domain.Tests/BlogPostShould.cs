using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class BlogPostShould
{
    private static readonly Title ValidTitle = new("TDD Is Not About Testing");
    private static readonly PostContent ValidContent = new("Some markdown content");
    private static readonly DateTime CreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void generate_slug_from_title_on_creation()
    {
        var post = BlogPost.Create(ValidTitle, ValidContent, CreatedAt);

        post.Slug.Should().Be(Slug.FromTitle(ValidTitle));
    }

    [Fact]
    public void start_in_draft_status_with_timestamps_set()
    {
        var post = BlogPost.Create(ValidTitle, ValidContent, CreatedAt);

        post.Status.Should().Be(PostStatus.Draft);
        post.CreatedAt.Should().Be(CreatedAt);
        post.UpdatedAt.Should().Be(CreatedAt);
    }
}
