using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Domain.Tests;

internal static class TestMeals
{
    public static IReadOnlyCollection<Meal> ValidPool()
    {
        return
        [
            new Meal(Guid.NewGuid(), "Oatmeal", MealType.Breakfast, false, 450, 25),
            new Meal(Guid.NewGuid(), "Yogurt Bowl", MealType.Breakfast, false, 320, 18),
            new Meal(Guid.NewGuid(), "Chicken Rice", MealType.Lunch, false, 620, 42),
            new Meal(Guid.NewGuid(), "Pasta", MealType.Lunch, false, 700, 28)
        ];
    }
}
