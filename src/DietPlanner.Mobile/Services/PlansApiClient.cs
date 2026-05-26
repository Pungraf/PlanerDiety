using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DietPlanner.Mobile.Services;

public interface IPlansApiClient
{
    Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default);

    Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, CancellationToken cancellationToken = default);

    Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, CancellationToken cancellationToken = default);
}

public sealed class PlansApiClient : IPlansApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ISessionStore _sessionStore;

    public PlansApiClient(HttpClient httpClient, ISessionStore sessionStore)
    {
        _httpClient = httpClient;
        _sessionStore = sessionStore;
    }

    public async Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "api/plans/current");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CurrentPlanDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Plans API returned an empty current plan response.");
    }

    public async Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
    {
        var endpoint = string.IsNullOrWhiteSpace(query)
            ? "api/meals"
            : $"api/meals?name={Uri.EscapeDataString(query.Trim())}";

        using var request = CreateRequest(HttpMethod.Get, endpoint);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MealSummaryDto[]>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Put,
            $"api/plans/current/days/{date:yyyy-MM-dd}/slots/{slotType}");
        request.Content = JsonContent.Create(new ReplaceMealRequest(mealId));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, "api/plans/current/copy-day");
        request.Content = JsonContent.Create(new CopyDayRequest(sourceDate.ToString("yyyy-MM-dd"), targetDate.ToString("yyyy-MM-dd")));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
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

    private sealed record ReplaceMealRequest(Guid MealId);

    private sealed record CopyDayRequest(string SourceDate, string TargetDate);
}

public sealed record CurrentPlanDto(Guid Id, string Status, string StartDate, IReadOnlyList<PlanDayDto> Days);

public sealed record PlanDayDto(string Date, IReadOnlyList<PlanMealSlotDto> Meals);

public sealed record PlanMealSlotDto(string SlotType, Guid? MealId, string Name, int Kcal, int Protein);

public sealed record MealSummaryDto(Guid Id, string Name, string Type, int Kcal, int Protein);
