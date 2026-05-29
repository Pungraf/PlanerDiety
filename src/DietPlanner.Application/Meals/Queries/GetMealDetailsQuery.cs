using DietPlanner.Application.Abstractions;

namespace DietPlanner.Application.Meals.Queries;

public sealed record GetMealDetailsQuery(Guid MealId);

public sealed class GetMealDetailsHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetMealDetailsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MealDetailsDto?> HandleAsync(GetMealDetailsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var meal = await _dbContext.FindMealDetailsByIdAsync(query.MealId, cancellationToken);
        if (meal is null)
        {
            return null;
        }

        var ingredientIds = meal.Ingredients.Select(ingredient => ingredient.IngredientId).Distinct().ToArray();
        var ingredients = await _dbContext.FindIngredientsByIdsAsync(ingredientIds, cancellationToken);
        var ingredientsById = ingredients.ToDictionary(ingredient => ingredient.Id);

        return MealDetailsDto.From(meal, ingredientsById);
    }
}

public sealed record MealDetailsDto(
    Guid Id,
    string Name,
    string Type,
    int Kcal,
    int Protein,
    string Description,
    IReadOnlyList<MealDetailsIngredientDto> Ingredients)
{
    public static MealDetailsDto From(
        DietPlanner.Domain.Entities.Meal meal,
        IReadOnlyDictionary<Guid, DietPlanner.Domain.Entities.Ingredient> ingredientsById)
    {
        ArgumentNullException.ThrowIfNull(meal);
        return new MealDetailsDto(
            meal.Id,
            meal.Name,
            meal.Type.ToString().ToLowerInvariant(),
            meal.Kcal,
            meal.Protein,
            string.Empty,
            meal.Ingredients
                .Select(ingredient => new MealDetailsIngredientDto(
                    ingredientsById.TryGetValue(ingredient.IngredientId, out var ingredientEntity)
                        ? ingredientEntity.Name
                        : "Unknown ingredient",
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.ShoppingCategory))
                .ToArray());
    }
}

public sealed record MealDetailsIngredientDto(string Name, decimal Quantity, string Unit, string Category);
