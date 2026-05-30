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
            new Meal(Guid.NewGuid(), "Egg Sandwich", MealType.Breakfast, false, 390, 24),
            new Meal(Guid.NewGuid(), "Cheesecake Jar", MealType.Breakfast, true, 410, 12),
            new Meal(Guid.NewGuid(), "Chicken Rice", MealType.Lunch, false, 620, 42),
            new Meal(Guid.NewGuid(), "Pasta", MealType.Lunch, false, 700, 28),
            new Meal(Guid.NewGuid(), "Beef Bowl", MealType.Lunch, false, 640, 36),
            new Meal(Guid.NewGuid(), "Salmon Salad", MealType.Dinner, false, 530, 35),
            new Meal(Guid.NewGuid(), "Turkey Wrap", MealType.Dinner, false, 480, 31)
        ];
    }

    public static IReadOnlyCollection<Meal> StandardPoolWithSingleBreakfast()
    {
        return
        [
            new Meal(Guid.NewGuid(), "Oatmeal", MealType.Breakfast, false, 450, 25),
            new Meal(Guid.NewGuid(), "Chicken Rice", MealType.Lunch, false, 620, 42),
            new Meal(Guid.NewGuid(), "Pasta", MealType.Lunch, false, 700, 28),
            new Meal(Guid.NewGuid(), "Salmon Salad", MealType.Dinner, false, 530, 35)
        ];
    }

    public static IReadOnlyCollection<Meal> BreakfastStylePoolWithTwoBreakfasts()
    {
        return
        [
            new Meal(Guid.NewGuid(), "Oatmeal", MealType.Breakfast, false, 450, 25),
            new Meal(Guid.NewGuid(), "Yogurt Bowl", MealType.Breakfast, false, 320, 18),
            new Meal(Guid.NewGuid(), "Cheesecake Jar", MealType.Breakfast, true, 410, 12),
            new Meal(Guid.NewGuid(), "Chicken Rice", MealType.Lunch, false, 620, 42),
            new Meal(Guid.NewGuid(), "Pasta", MealType.Lunch, false, 700, 28),
            new Meal(Guid.NewGuid(), "Salmon Salad", MealType.Dinner, false, 530, 35)
        ];
    }

    public static IReadOnlyCollection<Meal> StandardPoolWithoutDinner()
    {
        return
        [
            new Meal(Guid.NewGuid(), "Oatmeal", MealType.Breakfast, false, 450, 25),
            new Meal(Guid.NewGuid(), "Yogurt Bowl", MealType.Breakfast, false, 320, 18),
            new Meal(Guid.NewGuid(), "Egg Sandwich", MealType.Breakfast, false, 390, 24),
            new Meal(Guid.NewGuid(), "Chicken Rice", MealType.Lunch, false, 620, 42),
            new Meal(Guid.NewGuid(), "Pasta", MealType.Lunch, false, 700, 28)
        ];
    }

    public static IReadOnlyCollection<Meal> PoolWithSingleLunch()
    {
        return
        [
            new Meal(Guid.NewGuid(), "Oatmeal", MealType.Breakfast, false, 450, 25),
            new Meal(Guid.NewGuid(), "Yogurt Bowl", MealType.Breakfast, false, 320, 18),
            new Meal(Guid.NewGuid(), "Egg Sandwich", MealType.Breakfast, false, 390, 24),
            new Meal(Guid.NewGuid(), "Chicken Rice", MealType.Lunch, false, 620, 42),
            new Meal(Guid.NewGuid(), "Salmon Salad", MealType.Dinner, false, 530, 35)
        ];
    }
}
