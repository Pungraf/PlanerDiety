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
            catalog:
            [
                new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45),
                new MealSummaryDto(Guid.NewGuid(), "Oats Bowl", "breakfast", 500, 30),
                new MealSummaryDto(Guid.NewGuid(), "Salmon Potatoes", "dinner", 600, 35)
            ]);
        var viewModel = new MealSearchViewModel(
            plansClient,
            new InMemoryMealSearchContextStore
            {
                Current = new MealSearchContext(null, new DateOnly(2026, 5, 26), "breakfast", "Oats Bowl")
            },
            new RecordingNavigator(),
            new RejectingPromptService());

        await viewModel.LoadAsync();
        viewModel.SearchText = "sal";

        var result = Assert.Single(viewModel.Meals);
        Assert.Equal("Salmon Potatoes", result.Name);
        Assert.Equal(1, plansClient.CatalogCalls);
    }

    [Fact]
    public async Task ReplaceMealCommand_ShouldReplaceSelectedMealAndNavigateHome()
    {
        var selectedMeal = new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45);
        var plansClient = new FakePlansApiClient(catalog: [selectedMeal]);
        var navigator = new RecordingNavigator();
        var contextStore = new InMemoryMealSearchContextStore
        {
            Current = new MealSearchContext(null, new DateOnly(2026, 5, 26), "breakfast", "Oats Bowl")
        };
        var viewModel = new MealSearchViewModel(plansClient, contextStore, navigator, new RejectingPromptService());

        await viewModel.LoadAsync();
        await viewModel.ReplaceMealCommand.ExecuteAsync(viewModel.Meals.Single());

        Assert.Equal(new DateOnly(2026, 5, 26), plansClient.LastReplaceDate);
        Assert.Equal("breakfast", plansClient.LastReplaceSlotType);
        Assert.Equal(selectedMeal.Id, plansClient.LastReplaceMealId);
        Assert.False(plansClient.LastDeleteLinkedShoppingLists);
        Assert.Equal("//home", navigator.LastRoute);
        Assert.Null(contextStore.Current);
    }

    [Fact]
    public async Task ReplaceMealCommand_ShouldPreserveContext_WhenNavigationFails()
    {
        var selectedMeal = new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45);
        var plansClient = new FakePlansApiClient(catalog: [selectedMeal]);
        var navigator = new ThrowingNavigator(new InvalidOperationException("Navigation failed."));
        var contextStore = new InMemoryMealSearchContextStore
        {
            Current = new MealSearchContext(null, new DateOnly(2026, 5, 26), "breakfast", "Oats Bowl")
        };
        var viewModel = new MealSearchViewModel(plansClient, contextStore, navigator, new RejectingPromptService());

        await viewModel.LoadAsync();
        await viewModel.ReplaceMealCommand.ExecuteAsync(viewModel.Meals.Single());

        Assert.Equal("Navigation failed.", viewModel.ErrorMessage);
        Assert.NotNull(contextStore.Current);
        Assert.Equal(new DateOnly(2026, 5, 26), contextStore.Current!.Date);
        Assert.Equal("breakfast", contextStore.Current.SlotType);
    }

    [Fact]
    public async Task ReplaceMealAfterConflict_ShouldPromptAndRetryWithDeleteFlag()
    {
        var selectedMeal = new MealSummaryDto(Guid.NewGuid(), "Chicken Rice", "lunch", 700, 45);
        var plansClient = new FakePlansApiClient(catalog: [selectedMeal], throwConflictOnce: true);
        var contextStore = new InMemoryMealSearchContextStore
        {
            Current = new MealSearchContext(null, new DateOnly(2026, 5, 26), "breakfast", "Oats Bowl")
        };
        var viewModel = new MealSearchViewModel(plansClient, contextStore, new RecordingNavigator(), new AcceptingPromptService());

        await viewModel.LoadAsync();
        await viewModel.ReplaceMealCommand.ExecuteAsync(viewModel.Meals.Single());

        Assert.Equal([false, true], plansClient.ReplaceCalls);
    }

    private sealed class FakePlansApiClient : IPlansApiClient
    {
        private readonly IReadOnlyList<MealSummaryDto> _catalog;
        private readonly bool _throwConflictOnce;
        private bool _conflictThrown;

        public FakePlansApiClient(IReadOnlyList<MealSummaryDto> catalog, bool throwConflictOnce = false)
        {
            _catalog = catalog;
            _throwConflictOnce = throwConflictOnce;
        }

        public DateOnly? LastReplaceDate { get; private set; }

        public string? LastReplaceSlotType { get; private set; }

        public Guid? LastReplaceMealId { get; private set; }

        public bool? LastDeleteLinkedShoppingLists { get; private set; }

        public int CatalogCalls { get; private set; }

        public List<bool> ReplaceCalls { get; } = [];

        public Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task CopyDayAsync(Guid planId, DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<MealSummaryDto>> GetMealCatalogAsync(CancellationToken cancellationToken = default)
        {
            CatalogCalls++;
            return Task.FromResult<IReadOnlyList<MealSummaryDto>>(_catalog);
        }

        public Task<MealDetailsDto> GetMealDetailsAsync(Guid mealId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
        {
            ReplaceCalls.Add(deleteLinkedShoppingLists);
            LastDeleteLinkedShoppingLists = deleteLinkedShoppingLists;

            if (_throwConflictOnce && !_conflictThrown)
            {
                _conflictThrown = true;
                throw new LinkedShoppingListsExistException();
            }

            LastReplaceDate = date;
            LastReplaceSlotType = slotType;
            LastReplaceMealId = mealId;
            return Task.CompletedTask;
        }

        public Task ReplaceMealAsync(Guid planId, DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
        {
            return ReplaceMealAsync(date, slotType, mealId, deleteLinkedShoppingLists, cancellationToken);
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

        public Task GoToMainTabAsync(MainAppTab tab)
        {
            LastRoute = tab == MainAppTab.Home ? "//home" : "//main/shopping-list";
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

        public Task GoToMainTabAsync(MainAppTab tab)
        {
            return Task.FromException(_exception);
        }
    }

    private sealed class AcceptingPromptService : IUserPromptService
    {
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class RejectingPromptService : IUserPromptService
    {
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        {
            return Task.FromResult(false);
        }
    }
}
