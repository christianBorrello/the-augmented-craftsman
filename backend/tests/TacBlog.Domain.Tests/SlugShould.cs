using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class SlugShould
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("TDD Is Not About Testing!", "tdd-is-not-about-testing")]
    [InlineData("  Spaces  Everywhere  ", "spaces-everywhere")]
    [InlineData("Multiple---Hyphens", "multiple-hyphens")]
    [InlineData("Special@#$Characters", "specialcharacters")]
    [InlineData("Already-Valid-Slug", "already-valid-slug")]
    public void generate_url_safe_slug_from_title(string titleText, string expected)
    {
        var title = new Title(titleText);

        var slug = Slug.FromTitle(title);

        slug.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void reject_empty_or_whitespace_input(string? input)
    {
        var act = () => new Slug(input!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void reconstitute_from_existing_slug_string()
    {
        var slug = new Slug("hello-world");

        slug.Should().Be(new Slug("hello-world"));
        slug.ToString().Should().Be("hello-world");
    }
}
