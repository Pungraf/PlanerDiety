using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public sealed class WeeklyPlanLifecycleTests
{
    [Fact]
    public void CreateCurrent_ShouldSetCurrentStatus()
    {
        var plan = WeeklyPlan.CreateCurrent(Guid.NewGuid(), new DateOnly(2026, 5, 31), DinnerMode.BreakfastStyle);

        plan.Status.Should().Be(WeeklyPlanStatus.Current);
    }

    [Fact]
    public void CreateFuture_ShouldSetFutureStatus()
    {
        var plan = WeeklyPlan.CreateFuture(Guid.NewGuid(), new DateOnly(2026, 6, 7), DinnerMode.BreakfastStyle);

        plan.Status.Should().Be(WeeklyPlanStatus.Future);
    }

    [Fact]
    public void PromoteFutureToCurrent_ShouldChangeStatus()
    {
        var plan = WeeklyPlan.CreateFuture(Guid.NewGuid(), new DateOnly(2026, 6, 7), DinnerMode.BreakfastStyle);

        plan.PromoteFutureToCurrent();

        plan.Status.Should().Be(WeeklyPlanStatus.Current);
    }
}
