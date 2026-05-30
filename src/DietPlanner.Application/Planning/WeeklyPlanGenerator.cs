using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Planning;

public sealed class WeeklyPlanGenerator : IWeeklyPlanGenerator
{
    private readonly Random _random;

    public WeeklyPlanGenerator()
        : this(Random.Shared)
    {
    }

    internal WeeklyPlanGenerator(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

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
        var dinnerMeals = request.DinnerMode switch
        {
            DinnerMode.BreakfastStyle => breakfastMeals,
            DinnerMode.LunchStyle => lunchMeals,
            _ => meals.Where(meal => meal.Type == MealType.Dinner).ToArray()
        };

        EnsureEligiblePoolSize(breakfastMeals, 2, "At least two non-dessert breakfast meals are required.");
        EnsureEligiblePoolSize(lunchMeals, 1, "At least one lunch meal is required.");
        EnsureEligiblePoolSize(
            dinnerMeals,
            request.DinnerMode switch
            {
                DinnerMode.BreakfastStyle => 3,
                DinnerMode.LunchStyle => 2,
                _ => 1
            },
            request.DinnerMode switch
            {
                DinnerMode.BreakfastStyle => "At least three non-dessert breakfast meals are required for breakfast-style dinners.",
                DinnerMode.LunchStyle => "At least two lunch meals are required for lunch-style dinners.",
                _ => "At least one dinner meal is required."
            });

        var plan = WeeklyPlan.CreateDraft(request.UserId, request.StartDate, request.DinnerMode);
        var breakfastState = new PoolState();
        var lunchState = new PoolState();
        var dinnerState = ReferenceEquals(dinnerMeals, lunchMeals)
            ? lunchState
            : ReferenceEquals(dinnerMeals, breakfastMeals)
                ? breakfastState
                : new PoolState();
        HashSet<Guid> previousBreakfastFamilyMealIds = [];
        HashSet<Guid> previousLunchFamilyMealIds = [];
        HashSet<Guid> previousDinnerFamilyMealIds = [];

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var day = new DailyPlan(Guid.NewGuid(), request.StartDate.AddDays(dayOffset));
            HashSet<Guid> usedToday = [];

            var breakfast = DrawMeal(
                breakfastMeals,
                breakfastState,
                usedToday,
                previousBreakfastFamilyMealIds);
            usedToday.Add(breakfast.Id);

            var secondBreakfast = DrawMeal(
                breakfastMeals,
                breakfastState,
                usedToday,
                previousBreakfastFamilyMealIds);
            usedToday.Add(secondBreakfast.Id);

            var lunch = DrawMeal(
                lunchMeals,
                lunchState,
                usedToday,
                previousLunchFamilyMealIds);
            usedToday.Add(lunch.Id);

            day.AddSlot(new DailyMealSlot(Guid.NewGuid(), MealSlotType.Breakfast, breakfast.Id));
            day.AddSlot(new DailyMealSlot(
                Guid.NewGuid(),
                MealSlotType.SecondBreakfast,
                secondBreakfast.Id));
            day.AddSlot(new DailyMealSlot(Guid.NewGuid(), MealSlotType.Lunch, lunch.Id));

            var dinner = DrawMeal(
                dinnerMeals,
                dinnerState,
                usedToday,
                previousDinnerFamilyMealIds);
            usedToday.Add(dinner.Id);

            day.AddSlot(new DailyMealSlot(Guid.NewGuid(), MealSlotType.Dinner, dinner.Id));

            plan.AddDay(day);

            previousBreakfastFamilyMealIds =
            [
                breakfast.Id,
                secondBreakfast.Id
            ];

            previousLunchFamilyMealIds = [lunch.Id];
            previousDinnerFamilyMealIds = [dinner.Id];
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

    private Meal DrawMeal(
        IReadOnlyList<Meal> pool,
        PoolState state,
        IReadOnlySet<Guid> usedToday,
        IReadOnlySet<Guid> previousDayMealIds)
    {
        if (state.UsedInCycle.Count >= pool.Count)
        {
            state.UsedInCycle.Clear();
        }

        var candidateGroups = new[]
        {
            pool.Where(meal => !state.UsedInCycle.Contains(meal.Id) && !usedToday.Contains(meal.Id) && !previousDayMealIds.Contains(meal.Id)).ToArray(),
            pool.Where(meal => !state.UsedInCycle.Contains(meal.Id) && !usedToday.Contains(meal.Id)).ToArray(),
            pool.Where(meal => !usedToday.Contains(meal.Id) && !previousDayMealIds.Contains(meal.Id)).ToArray(),
            pool.Where(meal => !usedToday.Contains(meal.Id)).ToArray(),
            pool.Where(meal => !previousDayMealIds.Contains(meal.Id)).ToArray(),
            pool.ToArray()
        };

        var candidates = candidateGroups.First(group => group.Length > 0);
        var selected = candidates[_random.Next(candidates.Length)];
        state.UsedInCycle.Add(selected.Id);
        return selected;
    }

    private sealed class PoolState
    {
        public HashSet<Guid> UsedInCycle { get; } = [];
    }
}
