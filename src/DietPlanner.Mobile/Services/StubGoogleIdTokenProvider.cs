namespace DietPlanner.Mobile.Services;

public sealed class StubGoogleIdTokenProvider : IGoogleIdTokenProvider
{
    public Task<string> GetIdTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("replace-with-real-google-id-token");
    }
}
