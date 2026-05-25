using DietPlanner.Domain.Enums;

namespace DietPlanner.Domain.Entities;

public class DailyMealSlot
{
    public Guid Id { get; init; }

    public MealSlotType SlotType { get; private set; }

    public Guid? MealId { get; private set; }

    public DailyMealSlot(Guid id, MealSlotType slotType, Guid? mealId = null)
    {
        Id = id;
        SlotType = slotType;
        MealId = mealId;
    }
}
