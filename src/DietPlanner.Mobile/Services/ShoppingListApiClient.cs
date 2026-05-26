using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DietPlanner.Mobile.Services;

public interface IShoppingListApiClient
{
    Task<ShoppingListDto> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<ShoppingListDto> ToggleItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}

public sealed class ShoppingListApiClient : IShoppingListApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ISessionStore _sessionStore;

    public ShoppingListApiClient(HttpClient httpClient, ISessionStore sessionStore)
    {
        _httpClient = httpClient;
        _sessionStore = sessionStore;
    }

    public async Task<ShoppingListDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/shopping-lists/current");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ShoppingListDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopping list API returned an empty current shopping list response.");
    }

    public async Task<ShoppingListDto> ToggleItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"api/shopping-lists/current/items/{itemId}/toggle");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ShoppingListDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopping list API returned an empty toggle response.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var accessToken = _sessionStore.CurrentSession?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("No authenticated session is available.");
        }

        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}

public sealed record ShoppingListDto(Guid Id, IReadOnlyList<ShoppingListSummaryItemDto> SummaryItems);

public sealed record ShoppingListSummaryItemDto(Guid Id, string Name, decimal Quantity, string Unit, bool IsChecked);
