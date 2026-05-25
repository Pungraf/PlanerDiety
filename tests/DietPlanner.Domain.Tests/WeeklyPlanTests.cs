using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;
using System.Reflection;

namespace DietPlanner.Domain.Tests;

public class WeeklyPlanTests
{
    [Fact]
    public void ActivateDraft_ShouldMarkPlanAsActive()
    {
        var plan = WeeklyPlan.CreateDraft(Guid.NewGuid(), new DateOnly(2026, 5, 25), DinnerMode.BreakfastStyle);

        plan.Activate();

        plan.Status.Should().Be(WeeklyPlanStatus.Active);
    }

    [Fact]
    public void ActivateActivePlan_ShouldThrowInvalidOperationException()
    {
        var plan = WeeklyPlan.CreateDraft(Guid.NewGuid(), new DateOnly(2026, 5, 25), DinnerMode.BreakfastStyle);
        plan.Activate();

        var act = () => plan.Activate();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateDraft_WithEmptyUserId_ShouldThrowArgumentException()
    {
        var act = () => WeeklyPlan.CreateDraft(Guid.Empty, new DateOnly(2026, 5, 25), DinnerMode.BreakfastStyle);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateDraft_WithInvalidDinnerMode_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => WeeklyPlan.CreateDraft(Guid.NewGuid(), new DateOnly(2026, 5, 25), (DinnerMode)99);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DinnerModes_ShouldHaveDistinctValues()
    {
        ((int)DinnerMode.BreakfastStyle).Should().NotBe((int)DinnerMode.LunchStyle);
    }

    [Fact]
    public void AddDay_ShouldNotBePublic()
    {
        var method = typeof(WeeklyPlan).GetMethod(
            "AddDay",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.IsPublic.Should().BeFalse();
    }
}
