using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class TagColorShould
{
    [Fact]
    public void accept_valid_palette_colors()
    {
        foreach (var expected in TagColor.Palette)
        {
            var color = new TagColor(expected.Light, expected.Dark);

            color.Light.Should().Be(expected.Light);
            color.Dark.Should().Be(expected.Dark);
        }
    }

    [Fact]
    public void reject_empty_light_color()
    {
        var act = () => new TagColor("", "#E8845F");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void reject_empty_dark_color()
    {
        var act = () => new TagColor("#C2593A", "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void select_random_color_from_palette()
    {
        var color = TagColor.Random();

        TagColor.Palette.Should().Contain(color);
    }

    [Fact]
    public void restore_from_stored_light_value()
    {
        var original = TagColor.Palette[0];

        var restored = TagColor.FromStoredValue(original.Light);

        restored.Should().Be(original);
    }

    [Fact]
    public void reject_unknown_stored_value()
    {
        var act = () => TagColor.FromStoredValue("#000000");

        act.Should().Throw<ArgumentException>();
    }
}
