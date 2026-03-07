using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class TagShould
{
    [Theory]
    [InlineData("Clean Code", "clean-code")]
    [InlineData("TDD", "tdd")]
    [InlineData("  Hexagonal Architecture  ", "hexagonal-architecture")]
    [InlineData("C# Tips & Tricks!", "c-tips-tricks")]
    public void generate_slug_from_tag_name(string name, string expectedSlug)
    {
        var tag = Tag.Create(new TagName(name));

        tag.Slug.ToString().Should().Be(expectedSlug);
    }

    [Fact]
    public void have_unique_id()
    {
        var tag1 = Tag.Create(new TagName("TDD"));
        var tag2 = Tag.Create(new TagName("DDD"));

        tag1.Id.Should().NotBe(tag2.Id);
    }

    [Fact]
    public void be_equal_when_slugs_match()
    {
        var tag1 = Tag.Create(new TagName("Clean Code"));
        var tag2 = Tag.Create(new TagName("Clean Code"));

        tag1.Should().Be(tag2);
    }

    [Fact]
    public void not_be_equal_when_slugs_differ()
    {
        var tag1 = Tag.Create(new TagName("TDD"));
        var tag2 = Tag.Create(new TagName("DDD"));

        tag1.Should().NotBe(tag2);
    }

    [Theory]
    [InlineData("TDD", "Clean Code", "clean-code")]
    [InlineData("Old Name", "New Name", "new-name")]
    public void update_name_and_slug_on_rename(string originalName, string newName, string expectedSlug)
    {
        var tag = Tag.Create(new TagName(originalName));
        var originalId = tag.Id;

        tag.Rename(new TagName(newName));

        tag.Name.ToString().Should().Be(newName);
        tag.Slug.ToString().Should().Be(expectedSlug);
        tag.Id.Should().Be(originalId);
    }
}
