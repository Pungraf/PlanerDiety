namespace DietPlanner.Domain.Entities;

public class User
{
    public Guid Id { get; }

    public string Name { get; private set; }

    public string? Email { get; private set; }

    public string? GoogleSubject { get; private set; }

    public User(Guid id, string name)
        : this(id, name, null, null)
    {
    }

    public User(Guid id, string name, string? email, string? googleSubject)
    {
        Id = Guard.AgainstEmpty(id, nameof(id));
        Name = Guard.AgainstBlank(name, nameof(name));
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        GoogleSubject = string.IsNullOrWhiteSpace(googleSubject) ? null : googleSubject.Trim();
    }

    public void UpdateGoogleProfile(string name, string email, string googleSubject)
    {
        Name = Guard.AgainstBlank(name, nameof(name));
        Email = Guard.AgainstBlank(email, nameof(email));
        GoogleSubject = Guard.AgainstBlank(googleSubject, nameof(googleSubject));
    }
}
