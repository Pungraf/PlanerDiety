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

    private sealed record CurrentPlanResponse(Guid Id, string Status, string StartDate, string DinnerMode);
}
