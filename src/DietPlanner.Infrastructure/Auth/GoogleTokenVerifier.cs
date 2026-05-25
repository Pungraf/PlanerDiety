using DietPlanner.Application.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace DietPlanner.Infrastructure.Auth;

public sealed class GoogleTokenVerifier : IGoogleTokenVerifier
{
    private readonly string? _clientId;

    public GoogleTokenVerifier(IConfiguration configuration)
    {
        _clientId = configuration["GoogleAuth:ClientId"];
    }

    public async Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idToken);

        var validationSettings = new GoogleJsonWebSignature.ValidationSettings();
        if (!string.IsNullOrWhiteSpace(_clientId))
        {
            validationSettings.Audience = [_clientId];
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
        return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name ?? payload.Email);
    }
}
