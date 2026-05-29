using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Plans;

public class GetCurrentPlanTests
{
    [Fact]
    public async Task GetCurrentPlan_ShouldReturnCurrentDraftPlan()
    {
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/plans/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CurrentPlanResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("draft");
        payload.StartDate.Should().Be("2026-05-25");
    }

    [Fact]
    public async Task GetCurrentPlan_ShouldPreferActivePlanOverNewerDraft()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25)),
            PlansApiFactory.CreateDraftPlan(userId, new DateOnly(2026, 6, 1))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/plans/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CurrentPlanResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("active");
        payload.StartDate.Should().Be("2026-05-25");
    }

    [Fact]
    public async Task GetCurrentPlan_ShouldBootstrapInitialPlanWhenUserHasNone()
    {
        await using var app = await PlansApiFactory.WithUserOnlyAsync();
        await app.SeedMealsAsync(
        [
            new(TestMealIds.BreakfastOne, "Skyr bowl", DietPlanner.Domain.Enums.MealType.Breakfast, false, 420, 30),
            new(TestMealIds.BreakfastTwo, "Egg toast", DietPlanner.Domain.Enums.MealType.Breakfast, false, 450, 28),
            new(TestMealIds.BreakfastThree, "Cottage wrap", DietPlanner.Domain.Enums.MealType.Breakfast, false, 430, 31),
            new(TestMealIds.LunchOne, "Chicken rice", DietPlanner.Domain.Enums.MealType.Lunch, false, 650, 42)
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/plans/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CurrentPlanDetailsResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("active");
        payload.DinnerMode.Should().Be("breakfastStyle");
        payload.Days.Should().HaveCount(7);

        var savedPlan = await app.ReadCurrentPlanAsync();
        savedPlan.Should().NotBeNull();
        savedPlan!.Status.Should().Be(DietPlanner.Domain.Enums.WeeklyPlanStatus.Active);

        (await app.ReadShoppingListsAsync()).Should().BeEmpty();
    }

    private sealed record CurrentPlanResponse(Guid Id, string Status, string StartDate, string DinnerMode);
    private sealed record CurrentPlanDetailsResponse(Guid Id, string Status, string StartDate, string DinnerMode, IReadOnlyList<CurrentPlanDayResponse> Days);
    private sealed record CurrentPlanDayResponse(string Date, IReadOnlyList<CurrentPlanMealSlotResponse> Meals);
    private sealed record CurrentPlanMealSlotResponse(string SlotType, Guid? MealId, string Name, int Kcal, int Protein);

    private static class TestMealIds
    {
        public static readonly Guid BreakfastOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid BreakfastTwo = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid BreakfastThree = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid LunchOne = Guid.Parse("44444444-4444-4444-4444-444444444444");
    }
}
