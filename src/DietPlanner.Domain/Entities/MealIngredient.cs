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
        MealId = mealId;
        IngredientId = ingredientId;
        Quantity = quantity;
        Unit = unit;
        ShoppingCategory = shoppingCategory;
    }
}
