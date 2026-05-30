using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;

namespace DietPlanner.Mobile.Services;

public interface IShoppingListApiClient
{
    Task<IReadOnlyList<ShoppingListSummaryDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShoppingListSummaryDto>> ListAsync(Guid? planId, CancellationToken cancellationToken = default);

    Task<ShoppingListDetailsDto> GetDetailsAsync(Guid listId, CancellationToken cancellationToken = default);

    Task<ShoppingListDetailsDto> GetDetailsAsync(Guid listId, Guid? planId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShoppingListCreateDayOptionDto>> GetCreateOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShoppingListCreateDayOptionDto>> GetCreateOptionsAsync(Guid? planId, CancellationToken cancellationToken = default);

    Task<ShoppingListDetailsDto> CreateAsync(string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default);

    Task<ShoppingListDetailsDto> CreateAsync(Guid? planId, string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid listId, CancellationToken cancellationToken = default);

    Task<ShoppingListDetailsDto> ToggleItemAsync(Guid listId, Guid itemId, CancellationToken cancellationToken = default);
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

    public async Task<IReadOnlyList<ShoppingListSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await ListAsync(planId: null, cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingListSummaryDto>> ListAsync(Guid? planId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, WithPlanQuery("api/shopping-lists", planId));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ShoppingListSummaryDto[]>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<ShoppingListDetailsDto> GetDetailsAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        return await GetDetailsAsync(listId, planId: null, cancellationToken);
    }

    public async Task<ShoppingListDetailsDto> GetDetailsAsync(Guid listId, Guid? planId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, WithPlanQuery($"api/shopping-lists/{listId}", planId));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ShoppingListDetailsDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopping list API returned an empty details response.");
    }

    public async Task<IReadOnlyList<ShoppingListCreateDayOptionDto>> GetCreateOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetCreateOptionsAsync(planId: null, cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingListCreateDayOptionDto>> GetCreateOptionsAsync(Guid? planId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, WithPlanQuery("api/shopping-lists/create-options", planId));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ShoppingListCreateDayOptionDto[]>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<ShoppingListDetailsDto> CreateAsync(string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default)
    {
        return await CreateAsync(planId: null, name, ingredientKeys, cancellationToken);
    }

    public async Task<ShoppingListDetailsDto> CreateAsync(Guid? planId, string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, WithPlanQuery("api/shopping-lists", planId));
        request.Content = JsonContent.Create(new CreateShoppingListRequest(name, ingredientKeys));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ShoppingListDetailsDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Shopping list API returned an empty create response.");
    }

    public async Task DeleteAsync(Guid listId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"api/shopping-lists/{listId}");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ShoppingListDetailsDto> ToggleItemAsync(Guid listId, Guid itemId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"api/shopping-lists/{listId}/items/{itemId}/toggle");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ShoppingListDetailsDto>(cancellationToken: cancellationToken)
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

    private static string WithPlanQuery(string endpoint, Guid? planId)
    {
        return planId.HasValue
            ? $"{endpoint}?planId={planId.Value:D}"
            : endpoint;
    }
}

public sealed record ShoppingListSummaryDto(Guid Id, string Name, string CreatedAt, int ItemCount);

public sealed record ShoppingListDetailsDto(Guid Id, string Name, IReadOnlyList<ShoppingListSummaryItemDto>? Items);

public sealed record ShoppingListSummaryItemDto(Guid Id, string Name, decimal Quantity, string Unit, bool IsChecked);

public sealed record ShoppingListCreateDayOptionDto(string Date, IReadOnlyList<ShoppingListCreateMealOptionDto> Meals);

public sealed record ShoppingListCreateMealOptionDto(string SlotType, string MealName, IReadOnlyList<ShoppingListCreateIngredientOptionDto> Ingredients);

public sealed record ShoppingListCreateIngredientOptionDto(Guid IngredientId, string Name, decimal Quantity, string Unit, string Category, bool IsSelected);

public sealed record CreateShoppingListIngredientRequest(string Date, string SlotType, Guid IngredientId);

file sealed record CreateShoppingListRequest(string Name, IReadOnlyList<CreateShoppingListIngredientRequest> IngredientKeys);
