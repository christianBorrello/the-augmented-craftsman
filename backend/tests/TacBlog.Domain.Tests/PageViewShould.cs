using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class PageViewShould
{
    [Fact]
    public void create_with_correct_properties()
    {
        var slug = new Slug("my-post");
        var visitorId = new VisitorId(Guid.NewGuid());
        var viewedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

        var view = PageView.Create(slug, visitorId, viewedAt);

        view.Id.Should().NotBeEmpty();
        view.PostSlug.Should().Be(slug);
        view.VisitorId.Should().Be(visitorId);
        view.ViewedAtUtc.Should().Be(viewedAt);
    }

    [Fact]
    public void generate_unique_ids()
    {
        var slug = new Slug("my-post");
        var visitorId = new VisitorId(Guid.NewGuid());
        var now = DateTime.UtcNow;

        var view1 = PageView.Create(slug, visitorId, now);
        var view2 = PageView.Create(slug, visitorId, now);

        view1.Id.Should().NotBe(view2.Id);
    }
}
