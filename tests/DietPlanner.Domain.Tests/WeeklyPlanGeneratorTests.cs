using DietPlanner.Application.Planning;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class WeeklyPlanGeneratorTests
{
    [Fact]
    public void Generate_ShouldCreateSevenDaysWithFourSlotsPerDay()
    {
        var meals = TestMeals.ValidPool();
        var generator = new WeeklyPlanGenerator();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.BreakfastStyle,
            meals));

        plan.Days.Should().HaveCount(7);
        plan.Days.Should().OnlyContain(day => day.Slots.Count == 4);
    }
}
