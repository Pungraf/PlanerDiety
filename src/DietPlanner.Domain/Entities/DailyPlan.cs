namespace DietPlanner.Domain.Entities;

public class DailyPlan
{
    private readonly List<DailyMealSlot> _mealSlots = [];

    public Guid Id { get; }

    public DateOnly Date { get; private set; }

    public IReadOnlyCollection<DailyMealSlot> MealSlots => _mealSlots;

    public DailyPlan(Guid id, DateOnly date)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        Date = date;
    }

    public bool OverwriteMealsFrom(DailyPlan sourceDay)
    {
        ArgumentNullException.ThrowIfNull(sourceDay);

        var sourceSlots = sourceDay.MealSlots.ToDictionary(slot => slot.SlotType);
        var updated = false;

        foreach (var slot in _mealSlots)
        {
            sourceSlots.TryGetValue(slot.SlotType, out var sourceSlot);
            slot.AssignMeal(sourceSlot?.MealId);
            updated = true;
        }

        return updated;
    }

    internal void AddSlot(DailyMealSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        _mealSlots.Add(slot);
    }
}
