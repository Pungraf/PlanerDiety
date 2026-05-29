using System.Net;
using FluentAssertions;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task ActivateDraft_ShouldDeleteShoppingListsLinkedToPreviousActivePlan()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            PlansApiFactory.CreateActivePlan(userId, new DateOnly(2026, 5, 25)),
            PlansApiFactory.CreateDraftPlan(userId, new DateOnly(2026, 6, 1))
        ]);
        using var client = await app.CreateAuthenticatedClientAsync();

        var previousActivePlanId = (await app.ReadPlansAsync())
            .Single(plan => plan.StartDate == new DateOnly(2026, 5, 25))
            .Id;

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            await dbContext.ShoppingLists.AddAsync(new ShoppingList(Guid.NewGuid(), previousActivePlanId));
            await dbContext.SaveChangesAsync();
        }

        var response = await client.PostAsync("/api/plans/current/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var assertionScope = app.Services.CreateAsyncScope();
        var assertionDbContext = assertionScope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        (await assertionDbContext.ShoppingLists.AnyAsync(list => list.WeeklyPlanId == previousActivePlanId)).Should().BeFalse();
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
