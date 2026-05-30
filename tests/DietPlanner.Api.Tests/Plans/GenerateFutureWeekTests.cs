using System.Net;
using System.Net.Http.Json;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Plans;

public sealed class GenerateFutureWeekTests
{
    [Fact]
    public async Task GenerateFutureWeek_ShouldCreateFuturePlanOnce()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25))
        ]);
        await app.SeedMealsAsync(
        [
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Skyr bowl", MealType.Breakfast, false, 420, 30),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Egg toast", MealType.Breakfast, false, 450, 28),
            new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Cottage wrap", MealType.Breakfast, false, 430, 31),
            new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Chicken rice", MealType.Lunch, false, 650, 42)
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var first = await client.PostAsync("/api/plans/future/generate", null);
        var second = await client.PostAsync("/api/plans/future/generate", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await first.Content.ReadFromJsonAsync<PlanningStateResponse>();
        payload.Should().NotBeNull();
        payload!.FuturePlan.Should().NotBeNull();
    }

    private sealed record PlanningStateResponse(PlanResponse CurrentPlan, PlanResponse? FuturePlan, bool CanGenerateFutureWeek);

    private sealed record PlanResponse(Guid Id, string Status, string StartDate, string DinnerMode, IReadOnlyList<DayResponse> Days);

    private sealed record DayResponse(string Date, IReadOnlyList<MealSlotResponse> Meals);

    private sealed record MealSlotResponse(string SlotType, Guid? MealId, string Name, int Kcal, int Protein);
}
