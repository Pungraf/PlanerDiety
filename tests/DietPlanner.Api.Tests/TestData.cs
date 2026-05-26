using System.Reflection;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Api.Tests;

internal static class TestData
{
    public static readonly Guid BreakfastMealId = Guid.Parse("19a5fa1d-4b74-42a4-9b78-e67f96c9a1a5");
    public static readonly Guid LunchMealId = Guid.Parse("2f2c80bf-8c6b-4a3a-9120-d5fcb9c8919d");
    public static readonly Guid DinnerMealId = Guid.Parse("6c703857-a521-4577-bce0-840c93bc5d24");
    public static readonly Guid OatsShoppingListItemId = Guid.Parse("2f7cbb06-3dbc-45e8-ae3c-d0202a2c7271");
    public static readonly Guid ChickenShoppingListItemId = Guid.Parse("e08906a1-5d94-44cd-ac40-e76ff3aec0bd");
    public static readonly Guid TomatoShoppingListItemId = Guid.Parse("d0df7412-a8de-4fe5-b8b0-5f74f95ae347");
    public static readonly Guid TomatoSoupShoppingListItemId = Guid.Parse("80bde8b3-cb6b-4b67-b82a-295a44d7d88c");
    public static readonly Guid OatsIngredientId = Guid.Parse("b4b87663-d745-4769-a702-c657fd647b56");
    public static readonly Guid ChickenIngredientId = Guid.Parse("9243424c-0356-4a8d-9419-7f6d190f2bfd");
    public static readonly Guid TomatoIngredientId = Guid.Parse("eba6ad2d-7589-4e77-bfa3-c14e50921af7");

    public static IReadOnlyCollection<Ingredient> CreateIngredients()
    {
        return
        [
            new Ingredient(OatsIngredientId, "Oats", "g"),
            new Ingredient(ChickenIngredientId, "Chicken", "g"),
            new Ingredient(TomatoIngredientId, "Tomato", "g")
        ];
    }

    public static IReadOnlyCollection<Meal> CreateMeals()
    {
        var addIngredient = typeof(Meal).GetMethod("AddIngredient", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var breakfast = new Meal(BreakfastMealId, "Berry Oat Bowl", MealType.Breakfast, false, 420, 18);
        addIngredient.Invoke(breakfast, [new MealIngredient(BreakfastMealId, OatsIngredientId, 80, "g", "Pantry")]);

        var lunch = new Meal(LunchMealId, "Chicken Tomato Pasta", MealType.Lunch, false, 610, 35);
        addIngredient.Invoke(lunch, [new MealIngredient(LunchMealId, ChickenIngredientId, 140, "g", "Meat")]);
        addIngredient.Invoke(lunch, [new MealIngredient(LunchMealId, TomatoIngredientId, 120, "g", "Produce")]);

        var dinner = new Meal(DinnerMealId, "Tomato Soup", MealType.Dinner, false, 320, 12);
        addIngredient.Invoke(dinner, [new MealIngredient(DinnerMealId, TomatoIngredientId, 180, "g", "Produce")]);

        return [breakfast, lunch, dinner];
    }
}
