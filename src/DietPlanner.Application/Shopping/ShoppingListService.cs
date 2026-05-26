using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Shopping;

public sealed class ShoppingListService : IShoppingListService
{
    public ShoppingList GenerateForPlan(WeeklyPlan weeklyPlan, IReadOnlyCollection<Meal> meals)
    {
        ArgumentNullException.ThrowIfNull(weeklyPlan);
        ArgumentNullException.ThrowIfNull(meals);

        var mealsById = meals.ToDictionary(meal => meal.Id);
        var items = weeklyPlan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => mealsById.GetValueOrDefault(slot.MealId!.Value))
            .Where(meal => meal is not null)
            .SelectMany(meal => meal!.Ingredients)
            .GroupBy(ingredient => new { ingredient.IngredientId, ingredient.Unit })
            .OrderBy(group => group.Key.IngredientId)
            .ThenBy(group => group.Key.Unit)
            .Select(group => new ShoppingListItem(
                Guid.NewGuid(),
                group.Key.IngredientId,
                group.Sum(ingredient => ingredient.Quantity),
                group.Key.Unit))
            .ToArray();

        return ShoppingList.Create(weeklyPlan.Id, items);
    }
}
