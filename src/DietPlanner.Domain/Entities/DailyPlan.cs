namespace DietPlanner.Domain.Entities;

public class DailyPlan
{
    private readonly List<DailyMealSlot> _mealSlots = [];

    public Guid Id { get; init; }

    public DateOnly Date { get; private set; }

    public IReadOnlyCollection<DailyMealSlot> MealSlots => _mealSlots;

    public DailyPlan(Guid id, DateOnly date)
    {
        Id = id;
        Date = date;
    }
}
