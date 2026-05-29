using DietPlanner.Domain.Entities;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class MealIngredientTests
{
    [Fact]
    public void Constructor_ShouldAssignUnitAndShoppingCategory()
    {
        var ingredient = new MealIngredient(Guid.NewGuid(), Guid.NewGuid(), 2.5m, "pcs", "Produce");

        ingredient.Unit.Should().Be("pcs");
        ingredient.ShoppingCategory.Should().Be("Produce");
    }

    [Fact]
    public void Constructor_WithBlankShoppingCategory_ShouldThrowArgumentException()
    {
        var act = () => new MealIngredient(Guid.NewGuid(), Guid.NewGuid(), 2.5m, "pcs", " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldAllowZeroQuantityForNonShoppingIngredient()
    {
        var ingredient = new MealIngredient(Guid.NewGuid(), Guid.NewGuid(), 0m, "g", "Przyprawy");

        ingredient.Quantity.Should().Be(0m);
        ingredient.Unit.Should().Be("g");
    }
}
