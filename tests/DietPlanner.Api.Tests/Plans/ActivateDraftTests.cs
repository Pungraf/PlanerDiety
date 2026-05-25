using System.Net;
using FluentAssertions;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Api.Tests.Plans;

public class ActivateDraftTests
{
    [Fact]
    public async Task ActivateDraft_ShouldChangeStatusToActive()
    {
        await using var app = await PlansApiFactory.WithDraftPlanAsync();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/plans/current/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var plan = await app.ReadCurrentPlanAsync();
        plan.Should().NotBeNull();
        plan!.Status.Should().Be(WeeklyPlanStatus.Active);
    }

    [Fact]
    public async Task ActivateDraft_ShouldActivateLatestDraftWhenActivePlanAlreadyExists()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25)),
            PlansApiFactory.CreateDraftPlan(userId, new DateOnly(2026, 6, 1))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/plans/current/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var plans = await app.ReadPlansAsync();
        plans.Should().ContainSingle(plan => plan.StartDate == new DateOnly(2026, 6, 1) && plan.Status == WeeklyPlanStatus.Active);
        plans.Should().ContainSingle(plan => plan.StartDate == new DateOnly(2026, 5, 25) && plan.Status == WeeklyPlanStatus.Active);
    }

    [Fact]
    public async Task ActivateDraft_ShouldReturnNotFoundWhenNoDraftExists()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/plans/current/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
