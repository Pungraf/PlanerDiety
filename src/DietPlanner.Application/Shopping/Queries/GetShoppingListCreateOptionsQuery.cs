using DietPlanner.Application.Abstractions;

namespace DietPlanner.Application.Shopping.Queries;

public sealed record GetShoppingListCreateOptionsQuery(Guid UserId, Guid? PlanId = null);

public sealed class GetShoppingListCreateOptionsHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetShoppingListCreateOptionsHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ShoppingListCreateDayDto>> HandleAsync(
        GetShoppingListCreateOptionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = query.PlanId.HasValue
            ? await _dbContext.FindWeeklyPlanByIdAsync(query.UserId, query.PlanId.Value, cancellationToken)
            : await _dbContext.FindReadableWeeklyPlanAsync(query.UserId, cancellationToken);
        if (plan is null)
        {
            return [];
        }

        var mealIds = plan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => slot.MealId!.Value)
            .Distinct()
            .ToArray();

        var meals = await _dbContext.FindMealsByIdsAsync(mealIds, cancellationToken);
        var mealsById = meals.ToDictionary(meal => meal.Id);
        var ingredientIds = meals
            .SelectMany(meal => meal.Ingredients)
            .Select(ingredient => ingredient.IngredientId)
            .Distinct()
            .ToArray();
        var ingredients = await _dbContext.FindIngredientsByIdsAsync(ingredientIds, cancellationToken);
        var ingredientNames = ingredients.ToDictionary(ingredient => ingredient.Id, ingredient => ingredient.Name);

        return plan.Days
            .Select(day => new ShoppingListCreateDayDto(
                day.Date.ToString("yyyy-MM-dd"),
                day.MealSlots
                    .Where(slot => slot.MealId.HasValue && mealsById.ContainsKey(slot.MealId.Value))
                    .Select(slot =>
                    {
                        var meal = mealsById[slot.MealId!.Value];
                        return new ShoppingListCreateMealDto(
                            slot.SlotType.ToString().ToLowerInvariant(),
                            meal.Name,
                            meal.Ingredients
                                .Where(ingredient => ingredient.Quantity > 0m)
                                .Select(ingredient => new ShoppingListCreateIngredientDto(
                                    ingredient.IngredientId,
                                    ingredientNames.TryGetValue(ingredient.IngredientId, out var name) ? name : string.Empty,
                                    ingredient.Quantity,
                                    ingredient.Unit,
                                    ingredient.ShoppingCategory))
                                .ToArray());
                    })
                    .ToArray()))
            .ToArray();
    }
}

public sealed record ShoppingListCreateDayDto(string Date, IReadOnlyList<ShoppingListCreateMealDto> Meals);

public sealed record ShoppingListCreateMealDto(string SlotType, string MealName, IReadOnlyList<ShoppingListCreateIngredientDto> Ingredients);

public sealed record ShoppingListCreateIngredientDto(Guid IngredientId, string Name, decimal Quantity, string Unit, string Category);
