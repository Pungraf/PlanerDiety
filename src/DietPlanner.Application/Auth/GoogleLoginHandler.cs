using DietPlanner.Application.Abstractions;
using DietPlanner.Domain.Entities;

namespace DietPlanner.Application.Auth;

public sealed class GoogleLoginHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IGoogleTokenVerifier _googleTokenVerifier;
    private readonly ISessionTokenService _sessionTokenService;

    public GoogleLoginHandler(
        IApplicationDbContext dbContext,
        IGoogleTokenVerifier googleTokenVerifier,
        ISessionTokenService sessionTokenService)
    {
        _dbContext = dbContext;
        _googleTokenVerifier = googleTokenVerifier;
        _sessionTokenService = sessionTokenService;
    }

    public async Task<GoogleLoginResult> HandleAsync(GoogleLoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var googleUser = await _googleTokenVerifier.VerifyAsync(command.IdToken, cancellationToken);
        var user = await _dbContext.FindUserByGoogleSubjectAsync(googleUser.Subject, cancellationToken);

        if (user is null && googleUser.EmailVerified)
        {
            user = await _dbContext.FindUserByEmailAsync(googleUser.Email, cancellationToken);
        }

        if (user is null && !googleUser.EmailVerified)
        {
            throw new UnauthorizedAccessException("Google email must be verified before linking by email.");
        }

        if (user is null)
        {
            user = new User(Guid.NewGuid(), googleUser.Name, googleUser.Email, googleUser.Subject);
            await _dbContext.AddUserAsync(user, cancellationToken);
        }
        else
        {
            user.UpdateGoogleProfile(googleUser.Name, googleUser.Email, googleUser.Subject);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var accessToken = _sessionTokenService.CreateToken(user);
        return new GoogleLoginResult(accessToken);
    }
}

public sealed record GoogleLoginResult(string AccessToken);
