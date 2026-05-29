using DietPlanner.Mobile.Services;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class ApiBaseAddressResolverTests
{
    [Fact]
    public void Resolve_ShouldReturnConfiguredAbsoluteUri_WhenEnvironmentValueIsPresent()
    {
        var resolver = new ApiBaseAddressResolver();

        var result = resolver.Resolve("https://planer-diety-production.up.railway.app/");

        Assert.Equal(new Uri("https://planer-diety-production.up.railway.app/"), result);
    }

    [Fact]
    public void Resolve_ShouldFallBackToAndroidEmulatorAddress_WhenEnvironmentValueIsMissing()
    {
        var resolver = new ApiBaseAddressResolver();

        var result = resolver.Resolve(null);

        Assert.Equal(new Uri("https://10.0.2.2:5001/"), result);
    }
}
