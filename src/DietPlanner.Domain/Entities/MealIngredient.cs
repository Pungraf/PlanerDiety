namespace DietPlanner.Domain.Entities;

public class MealIngredient
{
    public Guid MealId { get; init; }

    public Guid IngredientId { get; init; }

    public decimal Quantity { get; private set; }

    public MealIngredient(Guid mealId, Guid ingredientId, decimal quantity)
    {
        MealId = mealId;
        IngredientId = ingredientId;
        Quantity = quantity;
    }
}
