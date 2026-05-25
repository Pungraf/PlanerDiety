using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class MealTests
{
    [Fact]
    public void Constructor_ShouldAssignDessertAndNutritionFields()
    {
        var meal = new Meal(Guid.NewGuid(), "Skyr Bowl", MealType.Breakfast, isDessert: true, kcal: 420, protein: 32);

        meal.IsDessert.Should().BeTrue();
        meal.Kcal.Should().Be(420);
        meal.Protein.Should().Be(32);
    }

    [Fact]
    public void Constructor_WithNegativeKcal_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => new Meal(Guid.NewGuid(), "Skyr Bowl", MealType.Breakfast, isDessert: false, kcal: -1, protein: 32);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithInvalidMealType_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => new Meal(Guid.NewGuid(), "Skyr Bowl", (MealType)99, isDessert: false, kcal: 420, protein: 32);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
