using DietPlanner.Domain.Entities;
using FluentAssertions;
using System.Reflection;

namespace DietPlanner.Domain.Tests;

public class DailyPlanTests
{
    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        var act = () => new DailyPlan(Guid.Empty, new DateOnly(2026, 5, 26));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddSlot_ShouldNotBePublic()
    {
        var method = typeof(DailyPlan).GetMethod(
            "AddSlot",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.IsPublic.Should().BeFalse();
    }
}
