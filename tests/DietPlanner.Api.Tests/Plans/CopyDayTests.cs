using System.Net;
using System.Net.Http.Json;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Plans;

public class CopyDayTests
{
    [Fact]
    public async Task CopyDay_ShouldOverwriteTargetDayAndRegenerateShoppingList()
    {
        await using var app = await PlansApiFactory.WithActivePlanAsync(userId =>
            PlansApiFactory.CreateActivePlan(
                userId,
                new DateOnly(2026, 5, 25),
                plan =>
                {
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 26), MealSlotType.Breakfast, TestData.BreakfastMealId);
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 26), MealSlotType.Dinner, TestData.LunchMealId);
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 27), MealSlotType.Breakfast, TestData.DinnerMealId);
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 27), MealSlotType.Dinner, TestData.BreakfastMealId);
                }));
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/plans/current/copy-day", new
        {
            SourceDate = "2026-05-26",
            TargetDate = "2026-05-27"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var plan = await app.ReadCurrentPlanAsync();
        plan.Should().NotBeNull();

        var targetDay = plan!.Days.Single(day => day.Date == new DateOnly(2026, 5, 27));
        targetDay.MealSlots.Single(slot => slot.SlotType == MealSlotType.Breakfast).MealId.Should().Be(TestData.BreakfastMealId);
        targetDay.MealSlots.Single(slot => slot.SlotType == MealSlotType.Dinner).MealId.Should().Be(TestData.LunchMealId);

        var shoppingList = await app.ReadShoppingListAsync();
        shoppingList.Should().BeNull();
    }

    [Fact]
    public async Task CopyDay_ShouldReturnConflict_WhenLinkedShoppingListsExistAndDeleteIsNotConfirmed()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/plans/current/copy-day", new
        {
            SourceDate = "2026-05-25",
            TargetDate = "2026-05-26",
            DeleteLinkedShoppingLists = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CopyDay_ShouldDeleteLinkedShoppingLists_WhenDeleteIsConfirmed()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/plans/current/copy-day", new
        {
            SourceDate = "2026-05-25",
            TargetDate = "2026-05-26",
            DeleteLinkedShoppingLists = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await app.ReadShoppingListsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CopyDay_ShouldOnlyUpdateSpecifiedPlan()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25)),
            PlansApiFactory.CreateDraftPlan(userId, new DateOnly(2026, 6, 1))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var plans = await app.ReadPlansAsync();
        var futurePlanId = plans.Single(plan => plan.StartDate == new DateOnly(2026, 6, 1)).Id;

        var response = await client.PostAsJsonAsync($"/api/plans/{futurePlanId}/copy-day", new
        {
            SourceDate = "2026-06-01",
            TargetDate = "2026-06-02"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedPlans = await app.ReadPlansAsync();
        updatedPlans.Should().ContainSingle(plan => plan.Id == futurePlanId);
    }
}
