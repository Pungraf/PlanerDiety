using System.Text.Json;

namespace DietPlanner.Mobile.Services;

public sealed record MobileAppSettings(Uri ApiBaseUrl, string GoogleWebClientId);

public sealed class MobileAppSettingsLoader
{
    public MobileAppSettings Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var payload = JsonSerializer.Deserialize<MobileAppSettingsDocument>(stream)
            ?? throw new InvalidOperationException("Mobile app settings file is empty or invalid.");

        if (string.IsNullOrWhiteSpace(payload.ApiBaseUrl))
        {
            throw new InvalidOperationException("Mobile app setting 'ApiBaseUrl' is required.");
        }

        if (!Uri.TryCreate(payload.ApiBaseUrl, UriKind.Absolute, out var apiBaseUrl))
        {
            throw new InvalidOperationException($"Mobile app setting 'ApiBaseUrl' is not a valid absolute URI: {payload.ApiBaseUrl}");
        }

        if (string.IsNullOrWhiteSpace(payload.GoogleWebClientId))
        {
            throw new InvalidOperationException("Mobile app setting 'GoogleWebClientId' is required.");
        }

        return new MobileAppSettings(apiBaseUrl, payload.GoogleWebClientId.Trim());
    }

    private sealed record MobileAppSettingsDocument(string? ApiBaseUrl, string? GoogleWebClientId);
}
