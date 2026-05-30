namespace DietPlanner.Mobile.Services;

public interface ISelectedPlanContextStore
{
    Guid? SelectedPlanId { get; set; }
}

public sealed class InMemorySelectedPlanContextStore : ISelectedPlanContextStore
{
    public Guid? SelectedPlanId { get; set; }
}
