using System.Reflection;
using DietPlanner.Application.Shopping;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class ShoppingListServiceTests
{
    [Fact]
    public void GenerateForPlan_ShouldAggregateIngredientQuantitiesAcrossAssignedMeals()
    {
        var plan = WeeklyPlan.CreateDraft(Guid.NewGuid(), new DateOnly(2026, 5, 25), DinnerMode.BreakfastStyle);
        var firstDay = new DailyPlan(Guid.NewGuid(), new DateOnly(2026, 5, 25));
        var secondDay = new DailyPlan(Guid.NewGuid(), new DateOnly(2026, 5, 26));
        AddSlot(firstDay, new DailyMealSlot(Guid.NewGuid(), MealSlotType.Breakfast, TestMealIds.Breakfast));
        AddSlot(firstDay, new DailyMealSlot(Guid.NewGuid(), MealSlotType.Dinner, TestMealIds.Dinner));
        AddSlot(secondDay, new DailyMealSlot(Guid.NewGuid(), MealSlotType.Breakfast, TestMealIds.Breakfast));
        AddDay(plan, firstDay);
        AddDay(plan, secondDay);

        var meals = new[]
        {
            CreateMeal(TestMealIds.Breakfast, MealType.Breakfast, (TestIngredientIds.Oats, 80m, "g", "Pantry")),
            CreateMeal(TestMealIds.Dinner, MealType.Dinner, (TestIngredientIds.Tomato, 120m, "g", "Produce"))
        };

        var shoppingList = new ShoppingListService().GenerateForPlan(plan, meals);

        shoppingList.WeeklyPlanId.Should().Be(plan.Id);
        shoppingList.Items.Should().HaveCount(2);
        shoppingList.Items.Should().ContainSingle(item => item.IngredientId == TestIngredientIds.Oats && item.Quantity == 160m && item.Unit == "g");
        shoppingList.Items.Should().ContainSingle(item => item.IngredientId == TestIngredientIds.Tomato && item.Quantity == 120m && item.Unit == "g");
    }

    private static Meal CreateMeal(Guid mealId, MealType type, params (Guid IngredientId, decimal Quantity, string Unit, string Category)[] ingredients)
    {
        var meal = new Meal(mealId, $"{type} meal", type, false, 100, 10);

        foreach (var ingredient in ingredients)
        {
            AddIngredient(meal, new MealIngredient(mealId, ingredient.IngredientId, ingredient.Quantity, ingredient.Unit, ingredient.Category));
        }

        return meal;
    }

    private static void AddDay(WeeklyPlan plan, DailyPlan day)
    {
        typeof(WeeklyPlan).GetMethod("AddDay", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plan, [day]);
    }

    private static void AddSlot(DailyPlan plan, DailyMealSlot slot)
    {
        typeof(DailyPlan).GetMethod("AddSlot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plan, [slot]);
    }

    private static void AddIngredient(Meal meal, MealIngredient ingredient)
    {
        typeof(Meal).GetMethod("AddIngredient", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(meal, [ingredient]);
    }

    private static class TestMealIds
    {
        public static readonly Guid Breakfast = Guid.Parse("0d2cbddc-7859-4bb3-8518-9c71c4b23301");
        public static readonly Guid Dinner = Guid.Parse("4ba63298-c7d7-4d96-a8bd-7f17514458d2");
    }

    private static class TestIngredientIds
    {
        public static readonly Guid Oats = Guid.Parse("7d6102b3-916f-43c0-9fcf-ac040cccf74f");
        public static readonly Guid Tomato = Guid.Parse("99cf07c4-8f95-4b38-aad4-c6bf95df8f43");
    }
}
