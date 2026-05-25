namespace DietPlanner.Domain.Entities;

public class Ingredient
{
    public Guid Id { get; init; }

    public string Name { get; private set; }

    public string Unit { get; private set; }

    public Ingredient(Guid id, string name, string unit)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        Name = Guard.AgainstBlank(name, nameof(name));
        Unit = Guard.AgainstBlank(unit, nameof(unit));
    }
}
