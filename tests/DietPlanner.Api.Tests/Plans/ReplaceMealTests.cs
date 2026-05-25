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
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
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
    }

    [Fact]
    public async Task ReplaceMeal_ShouldMutateLatestDraft_WhenActivePlanForSameWeekExists()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25)),
            PlansApiFactory.CreateDraftPlan(userId, new DateOnly(2026, 5, 25))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

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

        var activePlan = plans.Single(plan => plan.Status == WeeklyPlanStatus.Active);
        var draftPlan = plans.Single(plan => plan.Status == WeeklyPlanStatus.Draft);

        activePlan.Days.Single(day => day.Date == new DateOnly(2026, 5, 26))
            .MealSlots.Single(slot => slot.SlotType == MealSlotType.Breakfast)
            .MealId.Should().BeNull();

        draftPlan.Days.Single(day => day.Date == new DateOnly(2026, 5, 26))
            .MealSlots.Single(slot => slot.SlotType == MealSlotType.Breakfast)
            .MealId.Should().Be(TestData.LunchMealId);
    }
}
