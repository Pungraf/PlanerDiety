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

    internal void AddSlot(DailyMealSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        _mealSlots.Add(slot);
    }
}
