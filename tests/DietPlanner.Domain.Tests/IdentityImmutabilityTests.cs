using DietPlanner.Domain.Entities;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class IdentityImmutabilityTests
{
    public static IEnumerable<object[]> IdentityProperties()
    {
        yield return [typeof(Meal), nameof(Meal.Id)];
        yield return [typeof(MealIngredient), nameof(MealIngredient.MealId)];
        yield return [typeof(MealIngredient), nameof(MealIngredient.IngredientId)];
        yield return [typeof(Ingredient), nameof(Ingredient.Id)];
        yield return [typeof(User), nameof(User.Id)];
        yield return [typeof(ShoppingListItem), nameof(ShoppingListItem.Id)];
        yield return [typeof(ShoppingList), nameof(ShoppingList.Id)];
        yield return [typeof(DailyPlan), nameof(DailyPlan.Id)];
        yield return [typeof(DailyMealSlot), nameof(DailyMealSlot.Id)];
        yield return [typeof(WeeklyPlan), nameof(WeeklyPlan.Id)];
    }

    [Theory]
    [MemberData(nameof(IdentityProperties))]
    public void ValidatedIdentityProperties_ShouldNotHavePublicSetter(Type entityType, string propertyName)
    {
        var property = entityType.GetProperty(propertyName);

        property.Should().NotBeNull();
        (property!.SetMethod is null || !property.SetMethod.IsPublic).Should().BeTrue();
    }
}
