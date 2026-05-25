using DietPlanner.Domain.Entities;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class DailyPlanTests
{
    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        var act = () => new DailyPlan(Guid.Empty, new DateOnly(2026, 5, 26));

        act.Should().Throw<ArgumentException>();
    }
}
