using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class BlogPostShould
{
    private static readonly Title ValidTitle = new("TDD Is Not About Testing");
    private static readonly PostContent ValidContent = new("Some markdown content");
    private static readonly DateTime CreatedAt = new(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = CreatedAt.AddHours(2);

    private static BlogPost CreateDraftPost() =>
        BlogPost.Create(ValidTitle, ValidContent, CreatedAt);

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

    [Fact]
    public void update_title_without_changing_slug()
    {
        var post = CreateDraftPost();
        var originalSlug = post.Slug;
        var newTitle = new Title("New Title After Edit");

        post.UpdateTitle(newTitle, Later);

        post.Title.Should().Be(newTitle);
        post.Slug.Should().Be(originalSlug);
        post.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void update_content_and_advance_updated_at()
    {
        var post = CreateDraftPost();
        var newContent = new PostContent("Updated markdown content");

        post.UpdateContent(newContent, Later);

        post.Content.Should().Be(newContent);
        post.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void publish_draft_and_set_published_at()
    {
        var post = CreateDraftPost();

        post.Publish(Later);

        post.Status.Should().Be(PostStatus.Published);
        post.PublishedAt.Should().Be(Later);
        post.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void reject_publish_when_already_published()
    {
        var post = CreateDraftPost();
        post.Publish(Later);

        var act = () => post.Publish(Later.AddHours(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void add_and_remove_tags()
    {
        var post = CreateDraftPost();
        var tdd = Tag.Create(new TagName("TDD"));
        var ddd = Tag.Create(new TagName("DDD"));

        post.AddTag(tdd);
        post.AddTag(ddd);

        post.Tags.Should().HaveCount(2);
        post.Tags.Should().Contain(tdd);

        post.RemoveTag(tdd);

        post.Tags.Should().HaveCount(1);
        post.Tags.Should().NotContain(tdd);
    }

    [Fact]
    public void not_add_duplicate_tag()
    {
        var post = CreateDraftPost();
        var tag = Tag.Create(new TagName("TDD"));

        post.AddTag(tag);
        post.AddTag(tag);

        post.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void have_no_published_at_when_draft()
    {
        var post = CreateDraftPost();

        post.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void have_no_featured_image_by_default()
    {
        var post = CreateDraftPost();

        post.FeaturedImageUrl.Should().BeNull();
    }

    [Fact]
    public void set_featured_image_and_advance_updated_at()
    {
        var post = CreateDraftPost();
        var imageUrl = new FeaturedImageUrl("https://cdn.example.com/image.jpg");

        post.SetFeaturedImage(imageUrl, Later);

        post.FeaturedImageUrl.Should().Be(imageUrl);
        post.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void remove_featured_image_and_advance_updated_at()
    {
        var post = CreateDraftPost();
        var imageUrl = new FeaturedImageUrl("https://cdn.example.com/image.jpg");
        post.SetFeaturedImage(imageUrl, Later);
        var evenLater = Later.AddHours(1);

        post.RemoveFeaturedImage(evenLater);

        post.FeaturedImageUrl.Should().BeNull();
        post.UpdatedAt.Should().Be(evenLater);
    }

    [Fact]
    public void remove_featured_image_idempotently_when_no_image_set()
    {
        var post = CreateDraftPost();

        post.RemoveFeaturedImage(Later);

        post.FeaturedImageUrl.Should().BeNull();
        post.UpdatedAt.Should().Be(Later);
    }
}
