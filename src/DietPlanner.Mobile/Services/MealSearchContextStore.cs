namespace DietPlanner.Mobile.Services;

public sealed record MealSearchContext(Guid? PlanId, DateOnly Date, string SlotType, string CurrentMealName);

public interface IMealSearchContextStore
{
    MealSearchContext? Current { get; set; }
}

public sealed class InMemoryMealSearchContextStore : IMealSearchContextStore
{
    public MealSearchContext? Current { get; set; }
}
