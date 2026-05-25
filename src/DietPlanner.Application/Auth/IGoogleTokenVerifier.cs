namespace DietPlanner.Application.Auth;

public interface IGoogleTokenVerifier
{
    Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken);
}

public sealed record GoogleUserInfo(string Subject, string Email, string Name);
