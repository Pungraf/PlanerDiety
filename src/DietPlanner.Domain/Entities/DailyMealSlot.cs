using DietPlanner.Domain.Enums;

namespace DietPlanner.Domain.Entities;

public class DailyMealSlot
{
    public Guid Id { get; }

    public MealSlotType SlotType { get; private set; }

    public Guid? MealId { get; private set; }

    public DailyMealSlot(Guid id, MealSlotType slotType, Guid? mealId = null)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        SlotType = Guard.AgainstUndefinedEnum(slotType, nameof(slotType));
        MealId = mealId.HasValue ? Guard.AgainstEmpty(mealId.Value, nameof(mealId)) : null;
    }

    public void ReplaceMeal(Guid mealId)
    {
        MealId = Guard.AgainstEmpty(mealId, nameof(mealId));
    }

    public void AssignMeal(Guid? mealId)
    {
        MealId = mealId.HasValue ? Guard.AgainstEmpty(mealId.Value, nameof(mealId)) : null;
    }
}
