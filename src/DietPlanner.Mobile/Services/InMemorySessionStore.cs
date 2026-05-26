namespace DietPlanner.Mobile.Services;

public sealed class InMemorySessionStore : ISessionStore
{
    public AuthSession? CurrentSession { get; private set; }

    public void SetSession(AuthSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        CurrentSession = session;
    }
}
