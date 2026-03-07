using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class FeaturedImageUrlShould
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void reject_empty_or_whitespace_input(string? input)
    {
        var act = () => new FeaturedImageUrl(input!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("http://example.com/image.png")]
    [InlineData("ftp://example.com/image.png")]
    [InlineData("//example.com/image.png")]
    public void reject_non_https_urls(string input)
    {
        var act = () => new FeaturedImageUrl(input);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("https://")]
    [InlineData("https:// ")]
    public void reject_malformed_urls(string input)
    {
        var act = () => new FeaturedImageUrl(input);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void accept_valid_https_url()
    {
        var url = "https://ik.imagekit.io/abc/image.png";

        var featuredImageUrl = new FeaturedImageUrl(url);

        featuredImageUrl.Value.Should().Be(url);
    }

    [Fact]
    public void be_equal_when_values_match()
    {
        var url = "https://ik.imagekit.io/abc/image.png";

        var first = new FeaturedImageUrl(url);
        var second = new FeaturedImageUrl(url);

        first.Should().Be(second);
    }
}
