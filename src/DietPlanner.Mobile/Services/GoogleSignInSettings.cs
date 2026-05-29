namespace DietPlanner.Mobile.Services;

public sealed record GoogleSignInSettings(string WebClientId);

public sealed class GoogleSignInSettingsResolver
{
    public GoogleSignInSettings? TryResolve(string? webClientId)
    {
        if (string.IsNullOrWhiteSpace(webClientId))
        {
            return null;
        }

        return new GoogleSignInSettings(webClientId.Trim());
    }
}
