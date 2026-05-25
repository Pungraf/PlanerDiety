using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using FluentAssertions;

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
}
