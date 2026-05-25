using DietPlanner.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace DietPlanner.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly GoogleLoginHandler _googleLoginHandler;

    public AuthController(GoogleLoginHandler googleLoginHandler)
    {
        _googleLoginHandler = googleLoginHandler;
    }

    [HttpPost("google")]
    public async Task<ActionResult<LoginResponse>> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _googleLoginHandler.HandleAsync(new GoogleLoginCommand(request.IdToken), cancellationToken);
            return Ok(new LoginResponse(result.AccessToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}

public sealed record GoogleLoginRequest(string IdToken);

public sealed record LoginResponse(string AccessToken);
