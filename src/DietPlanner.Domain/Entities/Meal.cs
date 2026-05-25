using DietPlanner.Domain.Enums;

namespace DietPlanner.Domain.Entities;

public class Meal
{
    private readonly List<MealIngredient> _ingredients = [];

    public Guid Id { get; init; }

    public string Name { get; private set; }

    public MealType Type { get; private set; }

    public bool IsDessert { get; private set; }

    public int Kcal { get; private set; }

    public int Protein { get; private set; }

    public IReadOnlyCollection<MealIngredient> Ingredients => _ingredients;

    public Meal(Guid id, string name, MealType type, bool isDessert, int kcal, int protein)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        Name = Guard.AgainstBlank(name, nameof(name));
        Type = type;
        IsDessert = isDessert;
        Kcal = Guard.AgainstNegative(kcal, nameof(kcal));
        Protein = Guard.AgainstNegative(protein, nameof(protein));
    }
}
