using DietPlanner.Application.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace DietPlanner.Infrastructure.Auth;

public sealed class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly string _clientId;

    public GoogleTokenVerifier(IConfiguration configuration)
    {
        _clientId = configuration["GoogleAuth:ClientId"]
            ?? throw new InvalidOperationException("Google client ID configuration is missing: GoogleAuth:ClientId");

        if (string.IsNullOrWhiteSpace(_clientId))
        {
            throw new InvalidOperationException("Google client ID configuration is missing: GoogleAuth:ClientId");
        }
    }

    public async Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new UnauthorizedAccessException("Google ID token is required.");
        }

        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_clientId]
        };

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name ?? payload.Email, payload.EmailVerified);
        }
        catch (InvalidJwtException exception)
        {
            throw new UnauthorizedAccessException("Google ID token is invalid.", exception);
        }
    }
}
