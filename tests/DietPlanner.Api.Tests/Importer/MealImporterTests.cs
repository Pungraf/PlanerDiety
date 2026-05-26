using DietPlanner.Domain.Enums;
using DietPlanner.Importer.Import;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Importer;

public class MealImporterTests
{
    [Fact]
    public async Task Import_ShouldCreateMealsAndIngredientsFromCsv()
    {
        var csv = """
Name,Type,IsDessert,Kcal,Protein,IngredientName,Quantity,Unit,Category
Owsianka,Breakfast,false,450,25,Platki owsiane,80,g,Suche
""";
        var importer = new MealImporter();

        var result = await importer.ImportAsync(new StringReader(csv), CancellationToken.None);

        result.Meals.Should().ContainSingle();
        result.Ingredients.Should().ContainSingle();

        var meal = result.Meals.Single();
        meal.Name.Should().Be("Owsianka");
        meal.Type.Should().Be(MealType.Breakfast);
        meal.IsDessert.Should().BeFalse();
        meal.Kcal.Should().Be(450);
        meal.Protein.Should().Be(25);
        meal.Ingredients.Should().ContainSingle();
        meal.Ingredients.Single().IngredientName.Should().Be("Platki owsiane");
        meal.Ingredients.Single().Quantity.Should().Be(80);
        meal.Ingredients.Single().Unit.Should().Be("g");
        meal.Ingredients.Single().Category.Should().Be("Suche");

        var ingredient = result.Ingredients.Single();
        ingredient.Name.Should().Be("Platki owsiane");
        ingredient.Unit.Should().Be("g");
    }
}
