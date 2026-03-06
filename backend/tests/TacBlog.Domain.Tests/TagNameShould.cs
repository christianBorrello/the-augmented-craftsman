using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class TagNameShould
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void reject_empty_or_whitespace_input(string? input)
    {
        var act = () => new TagName(input!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void trim_whitespace_from_input()
    {
        var tagName = new TagName("  TDD  ");

        tagName.Should().Be(new TagName("TDD"));
    }

    [Fact]
    public void reject_input_exceeding_50_characters()
    {
        var longInput = new string('a', 51);

        var act = () => new TagName(longInput);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void accept_valid_input_up_to_50_characters()
    {
        var validInput = new string('a', 50);

        var tagName = new TagName(validInput);

        tagName.ToString().Should().Be(validInput);
    }
}
