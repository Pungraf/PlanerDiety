using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

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
}
