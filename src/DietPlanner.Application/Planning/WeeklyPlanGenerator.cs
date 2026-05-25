using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Planning;

public sealed class WeeklyPlanGenerator : IWeeklyPlanGenerator
{
    public WeeklyPlan Generate(WeeklyPlanGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Meals);

        if (request.Meals.Count == 0)
        {
            throw new ArgumentException("At least one meal is required.", nameof(request));
        }

        var meals = request.Meals.ToArray();

        if (meals.Any(meal => meal is null))
        {
            throw new ArgumentException("Meals cannot contain null entries.", nameof(request));
        }

        var breakfastMeals = meals
            .Where(meal => meal.Type == MealType.Breakfast && !meal.IsDessert)
            .ToArray();
        var lunchMeals = meals
            .Where(meal => meal.Type == MealType.Lunch)
            .ToArray();
        var dinnerMeals = request.DinnerMode == DinnerMode.LunchStyle
            ? lunchMeals
            : meals.Where(meal => meal.Type == MealType.Dinner).ToArray();

        EnsureEligiblePoolSize(breakfastMeals, 2, "At least two non-dessert breakfast meals are required.");
        EnsureEligiblePoolSize(lunchMeals, 1, "At least one lunch meal is required.");
        EnsureEligiblePoolSize(
            dinnerMeals,
            request.DinnerMode == DinnerMode.LunchStyle ? 2 : 1,
            request.DinnerMode == DinnerMode.LunchStyle
                ? "At least two lunch meals are required for lunch-style dinners."
                : "At least one dinner meal is required.");

        var plan = WeeklyPlan.CreateDraft(request.UserId, request.StartDate, request.DinnerMode);

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var day = new DailyPlan(Guid.NewGuid(), request.StartDate.AddDays(dayOffset));
            var breakfastStartIndex = dayOffset % breakfastMeals.Length;
            var lunch = lunchMeals[(dayOffset / 2) % lunchMeals.Length];

            day.AddSlot(new DailyMealSlot(Guid.NewGuid(), MealSlotType.Breakfast, breakfastMeals[breakfastStartIndex].Id));
            day.AddSlot(new DailyMealSlot(
                Guid.NewGuid(),
                MealSlotType.SecondBreakfast,
                breakfastMeals[(breakfastStartIndex + 1) % breakfastMeals.Length].Id));
            day.AddSlot(new DailyMealSlot(Guid.NewGuid(), MealSlotType.Lunch, lunch.Id));

            var dinner = request.DinnerMode == DinnerMode.LunchStyle
                ? dinnerMeals[((dayOffset / 2) + 1) % dinnerMeals.Length]
                : dinnerMeals[dayOffset % dinnerMeals.Length];

            day.AddSlot(new DailyMealSlot(Guid.NewGuid(), MealSlotType.Dinner, dinner.Id));

            plan.AddDay(day);
        }

        return plan;
    }

    private static void EnsureEligiblePoolSize(IReadOnlyCollection<Meal> meals, int minimumCount, string message)
    {
        if (meals.Count < minimumCount)
        {
            throw new InvalidOperationException(message);
        }
    }
}
