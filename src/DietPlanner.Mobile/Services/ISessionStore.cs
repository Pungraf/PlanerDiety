namespace DietPlanner.Mobile.Services;

public interface ISessionStore
{
    AuthSession? CurrentSession { get; }

    void SetSession(AuthSession session);
}
