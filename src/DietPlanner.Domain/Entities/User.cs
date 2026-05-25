namespace DietPlanner.Domain.Entities;

public class User
{
    public Guid Id { get; init; }

    public string Name { get; private set; }

    public User(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}
