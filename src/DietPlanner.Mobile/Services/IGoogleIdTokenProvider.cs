namespace DietPlanner.Mobile.Services;

public interface IGoogleIdTokenProvider
{
    Task<string> GetIdTokenAsync(CancellationToken cancellationToken = default);
}
