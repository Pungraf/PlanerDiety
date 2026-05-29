using DietPlanner.Domain.Enums;
using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Shopping;

public interface IShoppingListService
{
    ShoppingList GenerateForPlan(WeeklyPlan weeklyPlan, IReadOnlyCollection<Meal> meals);

    ShoppingList GenerateForSelections(
        WeeklyPlan weeklyPlan,
        IReadOnlyCollection<Meal> meals,
        string name,
        IReadOnlyCollection<Commands.SelectedShoppingIngredient> selectedIngredients);
}
