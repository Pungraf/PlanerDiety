using DietPlanner.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace DietPlanner.Api.Tests.Auth;

public class GoogleTokenVerifierTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenGoogleClientIdIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var act = () => new GoogleTokenVerifier(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*GoogleAuth:ClientId*");
    }
}
