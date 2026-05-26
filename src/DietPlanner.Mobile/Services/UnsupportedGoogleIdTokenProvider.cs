namespace DietPlanner.Mobile.Services;

public sealed class UnsupportedGoogleIdTokenProvider : IGoogleIdTokenProvider
{
    public Task<string> GetIdTokenAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Google sign-in is not configured for this build. Register a real IGoogleIdTokenProvider or use the DEBUG-only stub.");
    }
}
