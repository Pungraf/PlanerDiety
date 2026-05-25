using DietPlanner.Application.Planning;
using DietPlanner.Domain.Entities;
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
        plan.Days.Should().OnlyContain(day => day.MealSlots.Count == 4);
    }

    [Fact]
    public void Generate_WithNullMeals_ShouldThrowArgumentNullException()
    {
        var generator = new WeeklyPlanGenerator();

        var act = () => generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.BreakfastStyle,
            null!));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Generate_WithNullMealEntry_ShouldThrowArgumentException()
    {
        var generator = new WeeklyPlanGenerator();
        IReadOnlyCollection<Meal> meals =
        [
            TestMeals.ValidPool().First(),
            null!
        ];

        var act = () => generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.BreakfastStyle,
            meals));

        act.Should().Throw<ArgumentException>();
    }
}
