using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class DailyMealSlotTests
{
    [Fact]
    public void Constructor_WithInvalidSlotType_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => new DailyMealSlot(Guid.NewGuid(), (MealSlotType)99);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithEmptyMealId_ShouldThrowArgumentException()
    {
        var act = () => new DailyMealSlot(Guid.NewGuid(), MealSlotType.Breakfast, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
