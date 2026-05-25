using DietPlanner.Domain.Enums;

namespace DietPlanner.Domain.Entities;

public class Meal
{
    private readonly List<MealIngredient> _ingredients = [];

    public Guid Id { get; init; }

    public string Name { get; private set; }

    public MealType Type { get; private set; }

    public IReadOnlyCollection<MealIngredient> Ingredients => _ingredients;

    public Meal(Guid id, string name, MealType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }
}
