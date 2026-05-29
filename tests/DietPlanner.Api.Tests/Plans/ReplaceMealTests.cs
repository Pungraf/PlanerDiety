using System.Net;
using System.Net.Http.Json;
using DietPlanner.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DietPlanner.Api.Tests.Plans;

public class ReplaceMealTests
{
    [Fact]
    public async Task ReplaceMeal_ShouldAllowReplacingBreakfastWithLunchMeal()
    {
        await using var app = await PlansApiFactory.WithActivePlanAsync(userId =>
            PlansApiFactory.CreateActivePlan(
                userId,
                new DateOnly(2026, 5, 25),
                plan =>
                {
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 26), MealSlotType.Breakfast, TestData.BreakfastMealId);
                    PlansApiFactory.AssignMeal(plan, new DateOnly(2026, 5, 26), MealSlotType.Dinner, TestData.DinnerMealId);
                }));
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-26/slots/breakfast", new
        {
            MealId = TestData.LunchMealId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var plan = await app.ReadCurrentPlanAsync();
        plan.Should().NotBeNull();
        plan!.Days.Single(x => x.Date == new DateOnly(2026, 5, 26))
            .MealSlots.Single(x => x.SlotType == DietPlanner.Domain.Enums.MealSlotType.Breakfast)
            .MealId.Should().Be(TestData.LunchMealId);

        var shoppingList = await app.ReadShoppingListAsync();
        shoppingList.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceMeal_ShouldMutateTheSamePlanReturnedByGetCurrent()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25)),
            PlansApiFactory.CreateDraftPlan(userId, new DateOnly(2026, 6, 1))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var currentResponse = await client.GetAsync("/api/plans/current");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentPlan = await currentResponse.Content.ReadFromJsonAsync<CurrentPlanResponse>();
        currentPlan.Should().NotBeNull();

        var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-26/slots/breakfast", new
        {
            MealId = TestData.LunchMealId
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlanner.Infrastructure.Persistence.DietPlannerDbContext>();
        var plans = await dbContext.WeeklyPlans
            .Include(plan => plan.Days)
            .ThenInclude(day => day.MealSlots)
            .Where(plan => plan.UserId == app.User.Id)
            .ToListAsync();

        var mutatedPlan = plans.Single(plan => plan.Id == currentPlan!.Id);
        var untouchedDraft = plans.Single(plan => plan.Status == WeeklyPlanStatus.Draft);

        mutatedPlan.Days.Single(day => day.Date == new DateOnly(2026, 5, 26))
            .MealSlots.Single(slot => slot.SlotType == MealSlotType.Breakfast)
            .MealId.Should().Be(TestData.LunchMealId);

        untouchedDraft.Days.Single(day => day.Date == new DateOnly(2026, 6, 2))
            .MealSlots.Single(slot => slot.SlotType == MealSlotType.Breakfast)
            .MealId.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceMeal_ShouldReturnConflict_WhenLinkedShoppingListsExistAndDeleteIsNotConfirmed()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-25/slots/breakfast", new
        {
            MealId = TestData.DinnerMealId,
            DeleteLinkedShoppingLists = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReplaceMeal_ShouldDeleteLinkedShoppingLists_WhenDeleteIsConfirmed()
    {
        await using var app = await PlansApiFactory.WithShoppingListAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-25/slots/breakfast", new
        {
            MealId = TestData.DinnerMealId,
            DeleteLinkedShoppingLists = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await app.ReadShoppingListsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceMeal_ShouldReturnBadRequest_WhenMealIdIsMissing()
    {
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-26/slots/breakfast", new
        {
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReplaceMeal_ShouldReturnBadRequest_WhenMealIdIsEmpty()
    {
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-26/slots/breakfast", new
        {
            MealId = Guid.Empty
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReplaceMeal_ShouldReturnBadRequest_WhenSlotTypeIsAnUndefinedEnumValue()
    {
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/api/plans/current/days/2026-05-26/slots/999", new
        {
            MealId = TestData.LunchMealId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record CurrentPlanResponse(Guid Id, string Status, string StartDate, string DinnerMode);
}
