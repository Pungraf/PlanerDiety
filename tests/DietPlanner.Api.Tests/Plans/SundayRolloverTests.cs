using System.Reflection;
using DietPlanner.Application.Plans.Planning;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace DietPlanner.Api.Tests.Plans;

public sealed class SundayRolloverTests
{
    [Fact]
    public async Task GetPlanningState_ShouldPromoteFuturePlanOnSunday()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            CreateCurrentPlan(userId, new DateOnly(2026, 5, 31)),
            CreateFuturePlan(userId, new DateOnly(2026, 6, 7))
        ]);

        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<PlanningStateService>();

        var state = await service.GetAsync(app.User.Id, new DateOnly(2026, 6, 7), CancellationToken.None);

        state.Should().NotBeNull();
        state!.CurrentPlanId.Should().NotBeEmpty();
        state.FuturePlanId.Should().BeNull();

        var plans = await app.ReadPlansAsync();
        plans.Should().ContainSingle(plan => plan.StartDate == new DateOnly(2026, 6, 7) && plan.Status == WeeklyPlanStatus.Current);
        plans.Should().NotContain(plan => plan.StartDate == new DateOnly(2026, 5, 31));
    }

    [Fact]
    public async Task GetPlanningState_ShouldAutoGenerateCurrentPlanOnSundayWhenFutureMissing()
    {
        await using var app = await PlansApiFactory.WithPlansAsync(userId =>
        [
            CreateCurrentPlan(userId, new DateOnly(2026, 5, 31))
        ]);
        await app.SeedMealsAsync(
        [
            new Meal(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Skyr bowl", MealType.Breakfast, false, 420, 30),
            new Meal(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Egg toast", MealType.Breakfast, false, 450, 28),
            new Meal(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Cottage wrap", MealType.Breakfast, false, 430, 31),
            new Meal(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Chicken rice", MealType.Lunch, false, 650, 42)
        ]);

        await using var scope = app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<PlanningStateService>();

        var state = await service.GetAsync(app.User.Id, new DateOnly(2026, 6, 7), CancellationToken.None);

        state.Should().NotBeNull();
        state!.CurrentStartDate.Should().Be(new DateOnly(2026, 6, 7));
        state.FuturePlanId.Should().BeNull();

        var plans = await app.ReadPlansAsync();
        plans.Should().ContainSingle(plan => plan.StartDate == new DateOnly(2026, 6, 7) && plan.Status == WeeklyPlanStatus.Current);
    }

    private static WeeklyPlan CreateCurrentPlan(Guid userId, DateOnly startDate)
    {
        var plan = WeeklyPlan.CreateCurrent(userId, startDate, DinnerMode.BreakfastStyle);
        PopulateWeek(plan);
        return plan;
    }

    private static WeeklyPlan CreateFuturePlan(Guid userId, DateOnly startDate)
    {
        var plan = WeeklyPlan.CreateFuture(userId, startDate, DinnerMode.BreakfastStyle);
        PopulateWeek(plan);
        return plan;
    }

    private static void PopulateWeek(WeeklyPlan plan)
    {
        var addDay = typeof(WeeklyPlan).GetMethod("AddDay", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var addSlot = typeof(DailyPlan).GetMethod("AddSlot", BindingFlags.Instance | BindingFlags.NonPublic)!;

        for (var offset = 0; offset < 7; offset++)
        {
            var day = new DailyPlan(Guid.NewGuid(), plan.StartDate.AddDays(offset));
            addSlot.Invoke(day, [new DailyMealSlot(Guid.NewGuid(), MealSlotType.Breakfast)]);
            addSlot.Invoke(day, [new DailyMealSlot(Guid.NewGuid(), MealSlotType.Dinner)]);
            addDay.Invoke(plan, [day]);
        }
    }
}
