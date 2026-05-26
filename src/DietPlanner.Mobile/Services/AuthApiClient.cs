using System.Net.Http.Json;

namespace DietPlanner.Mobile.Services;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IGoogleIdTokenProvider _googleIdTokenProvider;

    public AuthApiClient(HttpClient httpClient, IGoogleIdTokenProvider googleIdTokenProvider)
    {
        _httpClient = httpClient;
        _googleIdTokenProvider = googleIdTokenProvider;
    }

    public async Task<AuthSession> LoginWithGoogleAsync(CancellationToken cancellationToken = default)
    {
        var idToken = await _googleIdTokenProvider.GetIdTokenAsync(cancellationToken);
        var response = await _httpClient.PostAsJsonAsync("api/auth/google", new GoogleLoginRequest(idToken), cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Auth API returned an empty login response.");

        return new AuthSession(payload.AccessToken);
    }

    private sealed record GoogleLoginRequest(string IdToken);

    private sealed record LoginResponse(string AccessToken);
}
