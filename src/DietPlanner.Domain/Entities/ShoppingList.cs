namespace DietPlanner.Domain.Entities;

public class ShoppingList
{
    private readonly List<ShoppingListItem> _items = [];

    public Guid Id { get; init; }

    public Guid WeeklyPlanId { get; private set; }

    public IReadOnlyCollection<ShoppingListItem> Items => _items;

    public ShoppingList(Guid id, Guid weeklyPlanId)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        WeeklyPlanId = Guard.AgainstEmpty(weeklyPlanId, nameof(weeklyPlanId));
    }
}
