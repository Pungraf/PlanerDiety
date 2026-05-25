using DietPlanner.Domain.Entities;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class ShoppingListTests
{
    [Fact]
    public void Constructor_WithEmptyWeeklyPlanId_ShouldThrowArgumentException()
    {
        var act = () => new ShoppingList(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
