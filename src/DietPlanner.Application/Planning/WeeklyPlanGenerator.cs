using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Planning;

public sealed class WeeklyPlanGenerator : IWeeklyPlanGenerator
{
    private static readonly MealSlotType[] DefaultSlots =
    [
        MealSlotType.Breakfast,
        MealSlotType.SecondBreakfast,
        MealSlotType.Lunch,
        MealSlotType.Dinner
    ];

    public WeeklyPlan Generate(WeeklyPlanGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Meals.Count == 0)
        {
            throw new ArgumentException("At least one meal is required.", nameof(request));
        }

        var meals = request.Meals.ToArray();
        var mealIndex = 0;
        var plan = WeeklyPlan.CreateDraft(request.UserId, request.StartDate, request.DinnerMode);

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var day = new DailyPlan(Guid.NewGuid(), request.StartDate.AddDays(dayOffset));

            foreach (var slotType in DefaultSlots)
            {
                var meal = meals[mealIndex % meals.Length];
                day.AddSlot(new DailyMealSlot(Guid.NewGuid(), slotType, meal.Id));
                mealIndex++;
            }

            plan.AddDay(day);
        }

        return plan;
    }
}
