using System.Text;
using DietPlanner.Mobile.Services;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class MobileAppSettingsLoaderTests
{
    [Fact]
    public void Load_ShouldReadApiBaseUrlAndGoogleWebClientId()
    {
        var json = """
{
  "ApiBaseUrl": "https://planer-diety-production.up.railway.app/",
  "GoogleWebClientId": "1234567890-example.apps.googleusercontent.com"
}
""";
        var loader = new MobileAppSettingsLoader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = loader.Load(stream);

        Assert.Equal(new Uri("https://planer-diety-production.up.railway.app/"), result.ApiBaseUrl);
        Assert.Equal("1234567890-example.apps.googleusercontent.com", result.GoogleWebClientId);
    }

    [Fact]
    public void Load_ShouldRejectMissingApiBaseUrl()
    {
        var json = """
{
  "GoogleWebClientId": "1234567890-example.apps.googleusercontent.com"
}
""";
        var loader = new MobileAppSettingsLoader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        Action act = () => loader.Load(stream);

        var exception = Assert.Throws<InvalidOperationException>(act);

        Assert.Contains("ApiBaseUrl", exception.Message);
    }
}
