using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Microsoft.Maui.ApplicationModel;

namespace DietPlanner.Mobile.Services;

public sealed class AndroidGoogleIdTokenProvider : IGoogleIdTokenProvider
{
    private readonly GoogleSignInSettings _settings;

    public AndroidGoogleIdTokenProvider(GoogleSignInSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> GetIdTokenAsync(CancellationToken cancellationToken = default)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No active Android activity is available for Google sign-in.");

        var signInOptions = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestEmail()
            .RequestIdToken(_settings.WebClientId)
            .Build();

        var client = GoogleSignIn.GetClient(activity, signInOptions);
        var resultIntent = await GoogleSignInActivityResultBroker.StartAsync(activity, client.SignInIntent, cancellationToken);
        var signInTask = GoogleSignIn.GetSignedInAccountFromIntent(resultIntent);

        try
        {
            var account = (GoogleSignInAccount?)signInTask.GetResult(Java.Lang.Class.FromType(typeof(ApiException)));
            if (account is null || string.IsNullOrWhiteSpace(account.IdToken))
            {
                throw new InvalidOperationException("Google sign-in completed without an ID token.");
            }

            return account.IdToken;
        }
        catch (ApiException exception)
        {
            throw new InvalidOperationException(
                $"Google sign-in failed with status code {(int)exception.StatusCode}.",
                exception);
        }
    }
}
