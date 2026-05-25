namespace DietPlanner.Domain.Entities;

public class MealIngredient
{
    public Guid MealId { get; init; }

    public Guid IngredientId { get; init; }

    public decimal Quantity { get; private set; }

    public string Unit { get; private set; }

    public string ShoppingCategory { get; private set; }

    public MealIngredient(Guid mealId, Guid ingredientId, decimal quantity, string unit, string shoppingCategory)
    {
        MealId = Guard.AgainstEmpty(mealId, nameof(mealId));
        IngredientId = Guard.AgainstEmpty(ingredientId, nameof(ingredientId));
        Quantity = Guard.AgainstNonPositive(quantity, nameof(quantity));
        Unit = Guard.AgainstBlank(unit, nameof(unit));
        ShoppingCategory = Guard.AgainstBlank(shoppingCategory, nameof(shoppingCategory));
    }
}
