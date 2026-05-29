namespace DietPlanner.Mobile.Services;

public sealed record MealDetailsContext(Guid MealId);

public interface IMealDetailsContextStore
{
    MealDetailsContext? Current { get; set; }
}

public sealed class InMemoryMealDetailsContextStore : IMealDetailsContextStore
{
    public MealDetailsContext? Current { get; set; }
}
