namespace DietPlanner.Mobile.Services;

// Temporary development-only provider until platform Google auth is implemented.
public sealed class StubGoogleIdTokenProvider : IGoogleIdTokenProvider
{
    public Task<string> GetIdTokenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("replace-with-real-google-id-token");
    }
}
