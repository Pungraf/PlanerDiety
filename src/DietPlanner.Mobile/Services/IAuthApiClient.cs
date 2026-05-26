namespace DietPlanner.Mobile.Services;

public interface IAuthApiClient
{
    Task<AuthSession> LoginWithGoogleAsync(CancellationToken cancellationToken = default);
}
