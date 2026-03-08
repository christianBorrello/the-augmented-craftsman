using FluentAssertions;
using TacBlog.Domain;
using Xunit;

namespace TacBlog.Domain.Tests;

public class VisitorIdShould
{
    [Fact]
    public void wrap_existing_guid()
    {
        var guid = Guid.NewGuid();

        var visitorId = new VisitorId(guid);

        visitorId.Value.Should().Be(guid);
    }

    [Fact]
    public void reject_empty_guid()
    {
        var act = () => new VisitorId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
