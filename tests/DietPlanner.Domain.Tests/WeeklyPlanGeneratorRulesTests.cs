using DietPlanner.Application.Planning;
using DietPlanner.Domain.Enums;
using FluentAssertions;

namespace DietPlanner.Domain.Tests;

public class WeeklyPlanGeneratorRulesTests
{
    [Fact]
    public void Generate_ShouldRepeatLunchForTwoConsecutiveDays()
    {
        var generator = new WeeklyPlanGenerator();
        var meals = TestMeals.ValidPool();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.Standard,
            meals));

        var day1Lunch = GetMealId(plan.Days.ElementAt(0), MealSlotType.Lunch);
        var day2Lunch = GetMealId(plan.Days.ElementAt(1), MealSlotType.Lunch);
        var day3Lunch = GetMealId(plan.Days.ElementAt(2), MealSlotType.Lunch);
        var day4Lunch = GetMealId(plan.Days.ElementAt(3), MealSlotType.Lunch);

        day2Lunch.Should().Be(day1Lunch);
        day4Lunch.Should().Be(day3Lunch);
        day3Lunch.Should().NotBe(day1Lunch);
    }

    [Fact]
    public void Generate_ShouldUseOnlyNonDessertBreakfastMealsForBreakfastSlots()
    {
        var generator = new WeeklyPlanGenerator();
        var meals = TestMeals.ValidPool();
        var allowedBreakfastIds = meals
            .Where(meal => meal.Type == MealType.Breakfast && !meal.IsDessert)
            .Select(meal => meal.Id)
            .ToHashSet();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.Standard,
            meals));

        var assignedBreakfastIds = plan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.SlotType is MealSlotType.Breakfast or MealSlotType.SecondBreakfast)
            .Select(slot => slot.MealId);

        assignedBreakfastIds.Should().OnlyContain(mealId => mealId.HasValue && allowedBreakfastIds.Contains(mealId.Value));
    }

    [Fact]
    public void Generate_WithStandardDinnerMode_ShouldUseDinnerMealsForDinnerSlot()
    {
        var generator = new WeeklyPlanGenerator();
        var meals = TestMeals.ValidPool();
        var allowedDinnerIds = meals
            .Where(meal => meal.Type == MealType.Dinner)
            .Select(meal => meal.Id)
            .ToHashSet();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.Standard,
            meals));

        var assignedDinnerIds = plan.Days
            .Select(day => GetMealId(day, MealSlotType.Dinner));

        assignedDinnerIds.Should().OnlyContain(mealId => allowedDinnerIds.Contains(mealId));
    }

    [Fact]
    public void Generate_WithLunchStyleDinnerMode_ShouldAvoidDailyDuplicates()
    {
        var generator = new WeeklyPlanGenerator();
        var meals = TestMeals.ValidPool();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.LunchStyle,
            meals));

        plan.Days.Should().OnlyContain(day =>
            day.MealSlots.Select(slot => slot.MealId).Distinct().Count() == day.MealSlots.Count);
    }

    [Fact]
    public void Generate_WithLunchStyleDinnerMode_ShouldUseLunchMealsForDinnerSlot()
    {
        var generator = new WeeklyPlanGenerator();
        var meals = TestMeals.ValidPool();
        var allowedDinnerIds = meals
            .Where(meal => meal.Type == MealType.Lunch)
            .Select(meal => meal.Id)
            .ToHashSet();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.LunchStyle,
            meals));

        var assignedDinnerIds = plan.Days
            .Select(day => GetMealId(day, MealSlotType.Dinner));

        assignedDinnerIds.Should().OnlyContain(mealId => allowedDinnerIds.Contains(mealId));
    }

    [Fact]
    public void Generate_WithTooFewBreakfastMealsForStandardMode_ShouldThrowInvalidOperationException()
    {
        var generator = new WeeklyPlanGenerator();

        var act = () => generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.Standard,
            TestMeals.StandardPoolWithSingleBreakfast()));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generate_WithLunchStyleDinnerMode_ShouldRepeatDinnerForTwoConsecutiveDays()
    {
        var generator = new WeeklyPlanGenerator();
        var meals = TestMeals.ValidPool();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.LunchStyle,
            meals));

        var day1Dinner = GetMealId(plan.Days.ElementAt(0), MealSlotType.Dinner);
        var day2Dinner = GetMealId(plan.Days.ElementAt(1), MealSlotType.Dinner);
        var day3Dinner = GetMealId(plan.Days.ElementAt(2), MealSlotType.Dinner);
        var day4Dinner = GetMealId(plan.Days.ElementAt(3), MealSlotType.Dinner);

        day2Dinner.Should().Be(day1Dinner);
        day4Dinner.Should().Be(day3Dinner);
        day3Dinner.Should().NotBe(day1Dinner);
    }

    [Fact]
    public void Generate_WithTooFewLunchMealsForLunchStyleMode_ShouldThrowInvalidOperationException()
    {
        var generator = new WeeklyPlanGenerator();

        var act = () => generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.LunchStyle,
            TestMeals.PoolWithSingleLunch()));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Generate_WithSingleEligibleLunchMeal_ShouldReuseItAcrossLunchPairs()
    {
        var generator = new WeeklyPlanGenerator();
        var meals = TestMeals.PoolWithSingleLunch();

        var plan = generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.Standard,
            meals));

        var expectedLunchId = meals.Single(meal => meal.Type == MealType.Lunch).Id;
        var assignedLunchIds = plan.Days
            .Select(day => GetMealId(day, MealSlotType.Lunch));

        assignedLunchIds.Should().OnlyContain(mealId => mealId == expectedLunchId);
    }

    [Fact]
    public void Generate_WithoutDinnerMealsInStandardMode_ShouldThrowInvalidOperationException()
    {
        var generator = new WeeklyPlanGenerator();

        var act = () => generator.Generate(new WeeklyPlanGenerationRequest(
            Guid.NewGuid(),
            new DateOnly(2026, 5, 25),
            DinnerMode.Standard,
            TestMeals.StandardPoolWithoutDinner()));

        act.Should().Throw<InvalidOperationException>();
    }

    private static Guid GetMealId(DietPlanner.Domain.Entities.DailyPlan day, MealSlotType slotType)
    {
        var mealId = day.MealSlots.Single(x => x.SlotType == slotType).MealId;
        mealId.Should().HaveValue();
        return mealId!.Value;
    }
}
