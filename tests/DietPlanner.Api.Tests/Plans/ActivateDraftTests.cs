using System.Net;
using FluentAssertions;

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
        plan!.Status.Should().Be(DietPlanner.Domain.Enums.WeeklyPlanStatus.Active);
    }
}
