namespace DietPlanner.Domain.Entities;

public class ShoppingList
{
    private readonly List<ShoppingListItem> _items = [];

    private ShoppingList()
    {
        Name = string.Empty;
        CreatedAt = DateTimeOffset.MinValue;
    }

    public Guid Id { get; }

    public Guid WeeklyPlanId { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<ShoppingListItem> Items => _items;

    public ShoppingList(
        Guid id,
        Guid weeklyPlanId,
        string name = "Shopping list",
        DateTimeOffset? createdAt = null)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        WeeklyPlanId = Guard.AgainstEmpty(weeklyPlanId, nameof(weeklyPlanId));
        Name = Guard.AgainstBlank(name, nameof(name));
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public static ShoppingList Create(Guid weeklyPlanId, IEnumerable<ShoppingListItem> items, string name = "Shopping list")
    {
        var shoppingList = new ShoppingList(Guid.NewGuid(), weeklyPlanId, name);
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
