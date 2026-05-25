using DietPlanner.Domain.Entities;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        var act = () => new User(Guid.Empty, "Anna");

        act.Should().Throw<ArgumentException>();
    }
}
