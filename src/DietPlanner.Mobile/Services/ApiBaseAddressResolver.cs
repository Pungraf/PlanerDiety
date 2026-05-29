namespace DietPlanner.Mobile.Services;

public sealed class ApiBaseAddressResolver
{
    private static readonly Uri FallbackAddress = new("https://10.0.2.2:5001/");

    public Uri Resolve(string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return FallbackAddress;
        }

        if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out var resolvedAddress))
        {
            throw new InvalidOperationException(
                $"Configured API base address '{configuredValue}' is not a valid absolute URI.");
        }

        return resolvedAddress;
    }
}
