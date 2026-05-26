using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;

namespace DietPlanner.Application.Plans.Queries;

public sealed record GetCurrentPlanQuery(Guid UserId);

public sealed class GetCurrentPlanHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetCurrentPlanHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentPlanDto?> HandleAsync(GetCurrentPlanQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plan = await _dbContext.FindReadableWeeklyPlanAsync(query.UserId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var mealIds = plan.Days
            .SelectMany(day => day.MealSlots)
            .Where(slot => slot.MealId.HasValue)
            .Select(slot => slot.MealId!.Value)
            .Distinct()
            .ToArray();
        var meals = await _dbContext.FindMealsByIdsAsync(mealIds, cancellationToken);

        return CurrentPlanDto.From(plan, meals);
    }
}

public sealed record CurrentPlanDto(Guid Id, DateOnly StartDate, string Status, string DinnerMode, IReadOnlyList<CurrentPlanDayDto> Days)
{
    public static CurrentPlanDto From(WeeklyPlan plan, IReadOnlyList<Meal> meals)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(meals);

        var mealLookup = meals.ToDictionary(meal => meal.Id);

        return new CurrentPlanDto(
            plan.Id,
            plan.StartDate,
            ToApiValue(plan.Status),
            ToApiValue(plan.DinnerMode),
            plan.Days
                .OrderBy(day => day.Date)
                .Select(day => CurrentPlanDayDto.From(day, mealLookup))
                .ToArray());
    }

    private static string ToApiValue(Enum value)
    {
        var name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}

public sealed record CurrentPlanDayDto(DateOnly Date, IReadOnlyList<CurrentPlanMealSlotDto> Meals)
{
    public static CurrentPlanDayDto From(DailyPlan day, IReadOnlyDictionary<Guid, Meal> mealLookup)
    {
        ArgumentNullException.ThrowIfNull(day);
        ArgumentNullException.ThrowIfNull(mealLookup);

        return new CurrentPlanDayDto(
            day.Date,
            day.MealSlots
                .OrderBy(slot => slot.SlotType)
                .Select(slot => CurrentPlanMealSlotDto.From(slot, mealLookup))
                .ToArray());
    }
}

public sealed record CurrentPlanMealSlotDto(string SlotType, Guid? MealId, string Name, int Kcal, int Protein)
{
    public static CurrentPlanMealSlotDto From(DailyMealSlot slot, IReadOnlyDictionary<Guid, Meal> mealLookup)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(mealLookup);

        var meal = slot.MealId.HasValue && mealLookup.TryGetValue(slot.MealId.Value, out var foundMeal)
            ? foundMeal
            : null;

        return new CurrentPlanMealSlotDto(
            ToApiValue(slot.SlotType),
            slot.MealId,
            meal?.Name ?? string.Empty,
            meal?.Kcal ?? 0,
            meal?.Protein ?? 0);
    }

    private static string ToApiValue(Enum value)
    {
        var name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
