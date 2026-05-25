namespace DietPlanner.Domain.Entities;

public class Ingredient
{
    public Guid Id { get; init; }

    public string Name { get; private set; }

    public string Unit { get; private set; }

    public Ingredient(Guid id, string name, string unit)
    {
        Id = id;
        Name = name;
        Unit = unit;
    }
}
