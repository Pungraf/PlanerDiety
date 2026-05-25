using DietPlanner.Domain.Entities;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class ShoppingListItemTests
{
    [Fact]
    public void Constructor_WithZeroQuantity_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => new ShoppingListItem(Guid.NewGuid(), Guid.NewGuid(), 0m, "g");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
