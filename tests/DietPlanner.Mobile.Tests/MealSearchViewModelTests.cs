using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class MealSearchViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldLoadMealsAndApplySearchFilter()
    {
        var plansClient = new FakePlansApiClient(
            new Dictionary<string, IReadOnlyList<MealSummaryDto>?>
            {
                [""] =
                [
                    new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45),
                    new MealSummaryDto(Guid.NewGuid(), "Oats Bowl", "breakfast", 500, 30),
                    new MealSummaryDto(Guid.NewGuid(), "Salmon Potatoes", "dinner", 600, 35)
                ],
                ["sal"] =
                [
                    new MealSummaryDto(Guid.NewGuid(), "Salmon Potatoes", "dinner", 600, 35)
                ]
            });
        var viewModel = new MealSearchViewModel(
            plansClient,
            new InMemoryMealSearchContextStore
            {
                Current = new MealSearchContext(new DateOnly(2026, 5, 26), "breakfast", "Oats Bowl")
            },
            new RecordingNavigator());

        await viewModel.LoadAsync();
        viewModel.SearchText = "sal";
        await plansClient.WaitForQueryAsync("sal");

        var result = Assert.Single(viewModel.Meals);
        Assert.Equal("Salmon Potatoes", result.Name);
        Assert.Equal(["", "sal"], plansClient.SearchQueries);
    }

    [Fact]
    public async Task ReplaceMealCommand_ShouldReplaceSelectedMealAndNavigateHome()
    {
        var selectedMeal = new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45);
        var plansClient = new FakePlansApiClient(new Dictionary<string, IReadOnlyList<MealSummaryDto>?>
        {
            [""] = [selectedMeal]
        });
        var navigator = new RecordingNavigator();
        var contextStore = new InMemoryMealSearchContextStore
        {
            Current = new MealSearchContext(new DateOnly(2026, 5, 26), "breakfast", "Oats Bowl")
        };
        var viewModel = new MealSearchViewModel(plansClient, contextStore, navigator);

        await viewModel.LoadAsync();
        await viewModel.ReplaceMealCommand.ExecuteAsync(viewModel.Meals.Single());

        Assert.Equal(new DateOnly(2026, 5, 26), plansClient.LastReplaceDate);
        Assert.Equal("breakfast", plansClient.LastReplaceSlotType);
        Assert.Equal(selectedMeal.Id, plansClient.LastReplaceMealId);
        Assert.Equal("//home", navigator.LastRoute);
        Assert.Null(contextStore.Current);
    }

    [Fact]
    public async Task ReplaceMealCommand_ShouldPreserveContext_WhenNavigationFails()
    {
        var selectedMeal = new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45);
        var plansClient = new FakePlansApiClient(new Dictionary<string, IReadOnlyList<MealSummaryDto>?>
        {
            [""] = [selectedMeal]
        });
        var navigator = new ThrowingNavigator(new InvalidOperationException("Navigation failed."));
        var contextStore = new InMemoryMealSearchContextStore
        {
            Current = new MealSearchContext(new DateOnly(2026, 5, 26), "breakfast", "Oats Bowl")
        };
        var viewModel = new MealSearchViewModel(plansClient, contextStore, navigator);

        await viewModel.LoadAsync();
        await viewModel.ReplaceMealCommand.ExecuteAsync(viewModel.Meals.Single());

        Assert.Equal("Navigation failed.", viewModel.ErrorMessage);
        Assert.NotNull(contextStore.Current);
        Assert.Equal(new DateOnly(2026, 5, 26), contextStore.Current!.Date);
        Assert.Equal("breakfast", contextStore.Current.SlotType);
    }

    private sealed class FakePlansApiClient : IPlansApiClient
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<MealSummaryDto>?> _mealsByQuery;
        private readonly Dictionary<string, TaskCompletionSource<bool>> _queryWaiters = [];

        public FakePlansApiClient(IReadOnlyDictionary<string, IReadOnlyList<MealSummaryDto>?> mealsByQuery)
        {
            _mealsByQuery = mealsByQuery;
        }

        public DateOnly? LastReplaceDate { get; private set; }

        public string? LastReplaceSlotType { get; private set; }

        public Guid? LastReplaceMealId { get; private set; }

        public List<string> SearchQueries { get; } = [];

        public Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
        {
            var normalizedQuery = query ?? string.Empty;
            SearchQueries.Add(normalizedQuery);

            if (_queryWaiters.TryGetValue(normalizedQuery, out var waiter))
            {
                waiter.TrySetResult(true);
            }

            return Task.FromResult(_mealsByQuery.TryGetValue(normalizedQuery, out var meals)
                ? meals ?? []
                : []);
        }

        public Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, CancellationToken cancellationToken = default)
        {
            LastReplaceDate = date;
            LastReplaceSlotType = slotType;
            LastReplaceMealId = mealId;
            return Task.CompletedTask;
        }

        public async Task WaitForQueryAsync(string query)
        {
            if (SearchQueries.Contains(query))
            {
                return;
            }

            var waiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queryWaiters[query] = waiter;
            var completedTask = await Task.WhenAny(waiter.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.True(completedTask == waiter.Task, $"Expected backend search query '{query}' to be issued.");
        }
    }

    private sealed class RecordingNavigator : IAppNavigator
    {
        public string? LastRoute { get; private set; }

        public Task GoToAsync(string route)
        {
            LastRoute = route;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingNavigator : IAppNavigator
    {
        private readonly Exception _exception;

        public ThrowingNavigator(Exception exception)
        {
            _exception = exception;
        }

        public Task GoToAsync(string route)
        {
            return Task.FromException(_exception);
        }
    }
}
