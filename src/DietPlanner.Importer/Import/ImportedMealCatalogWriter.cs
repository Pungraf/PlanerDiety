using DietPlanner.Domain.Entities;
using DietPlanner.Infrastructure.Persistence;

namespace DietPlanner.Importer.Import;

public sealed class ImportedMealCatalogWriter
{
    public async Task<MealCatalogImportSummary> WriteAsync(
        DietPlannerDbContext dbContext,
        MealImportResult result,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(result);

        var ingredientIds = result.Ingredients.ToDictionary(
            ingredient => new IngredientKey(ingredient.Name, ingredient.Unit),
            _ => Guid.NewGuid());

        var ingredients = result.Ingredients
            .Select(ingredient => new Ingredient(
                ingredientIds[new IngredientKey(ingredient.Name, ingredient.Unit)],
                ingredient.Name,
                ingredient.Unit))
            .ToList();

        var meals = new List<Meal>(result.Meals.Count);
        var mealIngredients = new List<MealIngredient>();

        foreach (var importedMeal in result.Meals)
        {
            var mealId = Guid.NewGuid();
            meals.Add(new Meal(
                mealId,
                importedMeal.Name,
                importedMeal.Type,
                importedMeal.IsDessert,
                importedMeal.Kcal,
                importedMeal.Protein));

            foreach (var importedIngredient in importedMeal.Ingredients)
            {
                mealIngredients.Add(new MealIngredient(
                    mealId,
                    ingredientIds[new IngredientKey(importedIngredient.IngredientName, importedIngredient.Unit)],
                    importedIngredient.Quantity,
                    importedIngredient.Unit,
                    importedIngredient.Category));
            }
        }

        await dbContext.Ingredients.AddRangeAsync(ingredients, cancellationToken);
        await dbContext.Meals.AddRangeAsync(meals, cancellationToken);
        await dbContext.MealIngredients.AddRangeAsync(mealIngredients, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MealCatalogImportSummary(meals.Count, ingredients.Count, mealIngredients.Count);
    }

    private sealed record IngredientKey(string Name, string Unit);
}

public sealed record MealCatalogImportSummary(int MealCount, int IngredientCount, int MealIngredientCount);
