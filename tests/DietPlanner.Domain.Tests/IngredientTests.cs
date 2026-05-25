using DietPlanner.Domain.Entities;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class IngredientTests
{
    [Fact]
    public void Constructor_WithBlankName_ShouldThrowArgumentException()
    {
        var act = () => new Ingredient(Guid.NewGuid(), " ", "g");

        act.Should().Throw<ArgumentException>();
    }
}
