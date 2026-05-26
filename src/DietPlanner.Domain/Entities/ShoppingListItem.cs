namespace DietPlanner.Domain.Entities;

public class ShoppingListItem
{
    public Guid Id { get; }

    public Guid IngredientId { get; private set; }

    public decimal Quantity { get; private set; }

    public string Unit { get; private set; }

    public bool IsChecked { get; private set; }

    public ShoppingListItem(Guid id, Guid ingredientId, decimal quantity, string unit, bool isChecked = false)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        IngredientId = Guard.AgainstEmpty(ingredientId, nameof(ingredientId));
        Quantity = Guard.AgainstNonPositive(quantity, nameof(quantity));
        Unit = Guard.AgainstBlank(unit, nameof(unit));
        IsChecked = isChecked;
    }

    public void ToggleChecked()
    {
        IsChecked = !IsChecked;
    }
}
