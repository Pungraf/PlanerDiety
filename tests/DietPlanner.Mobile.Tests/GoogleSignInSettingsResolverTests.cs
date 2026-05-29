using DietPlanner.Mobile.Services;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class GoogleSignInSettingsResolverTests
{
    [Fact]
    public void TryResolve_ShouldReturnSettings_WhenWebClientIdIsPresent()
    {
        var resolver = new GoogleSignInSettingsResolver();

        var result = resolver.TryResolve(" 1234567890-example.apps.googleusercontent.com ");

        Assert.NotNull(result);
        Assert.Equal("1234567890-example.apps.googleusercontent.com", result.WebClientId);
    }

    [Fact]
    public void TryResolve_ShouldReturnNull_WhenWebClientIdIsMissing()
    {
        var resolver = new GoogleSignInSettingsResolver();

        var result = resolver.TryResolve("   ");

        Assert.Null(result);
    }
}
