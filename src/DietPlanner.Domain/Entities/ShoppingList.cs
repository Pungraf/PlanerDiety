namespace DietPlanner.Domain.Entities;

public class ShoppingList
{
    private readonly List<ShoppingListItem> _items = [];

    public Guid Id { get; }

    public Guid WeeklyPlanId { get; private set; }

    public IReadOnlyCollection<ShoppingListItem> Items => _items;

    public ShoppingList(Guid id, Guid weeklyPlanId)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        WeeklyPlanId = Guard.AgainstEmpty(weeklyPlanId, nameof(weeklyPlanId));
    }

    public static ShoppingList Create(Guid weeklyPlanId, IEnumerable<ShoppingListItem> items)
    {
        var shoppingList = new ShoppingList(Guid.NewGuid(), weeklyPlanId);
        shoppingList.ReplaceItems(items);
        return shoppingList;
    }

    public void ReplaceItems(IEnumerable<ShoppingListItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _items.Clear();
        _items.AddRange(items);
    }
}
