using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DietPlanner.Application.Auth;
using DietPlanner.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DietPlanner.Infrastructure.Auth;

public sealed class JwtSessionTokenService : ISessionTokenService
{
    private const string DefaultIssuer = "DietPlanner";
    private const string DefaultAudience = "DietPlanner.Client";
    private const string DefaultSigningKey = "DietPlanner-Development-Key-Change-Me-12345";

    private readonly IConfiguration _configuration;

    public JwtSessionTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuer = _configuration["Jwt:Issuer"] ?? DefaultIssuer;
        var audience = _configuration["Jwt:Audience"] ?? DefaultAudience;
        var signingKey = _configuration["Jwt:Key"] ?? DefaultSigningKey;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Name)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
