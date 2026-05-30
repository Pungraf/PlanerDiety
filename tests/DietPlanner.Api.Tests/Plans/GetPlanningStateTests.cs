using System.Net;
using System.Net.Http.Json;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Api.Tests.Plans;

public sealed class GetPlanningStateTests
{
    [Fact]
    public async Task GetPlanningState_ShouldReturnCurrentAndFuturePlans()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            CreateCurrentPlan(userId, new DateOnly(2026, 5, 25)),
            CreateFuturePlan(userId, new DateOnly(2026, 6, 1))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/plans/state");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<PlanningStateResponse>();
        payload.Should().NotBeNull();
        payload!.CurrentPlan.StartDate.Should().Be("2026-05-25");
        payload.FuturePlan.Should().NotBeNull();
        payload.FuturePlan!.StartDate.Should().Be("2026-06-01");
    }

    private static WeeklyPlan CreateCurrentPlan(Guid userId, DateOnly startDate)
    {
        var plan = PlansApiFactory.CreateDraftPlan(userId, startDate);
        plan.MarkDraftAsCurrent();
        return plan;
    }

    private static WeeklyPlan CreateFuturePlan(Guid userId, DateOnly startDate)
    {
        var plan = PlansApiFactory.CreateDraftPlan(userId, startDate);
        plan.MarkDraftAsFuture();
        return plan;
    }

    private sealed record PlanningStateResponse(PlanResponse CurrentPlan, PlanResponse? FuturePlan, bool CanGenerateFutureWeek);

    private sealed record PlanResponse(Guid Id, string Status, string StartDate, string DinnerMode, IReadOnlyList<DayResponse> Days);

    private sealed record DayResponse(string Date, IReadOnlyList<MealSlotResponse> Meals);

    private sealed record MealSlotResponse(string SlotType, Guid? MealId, string Name, int Kcal, int Protein);
}
