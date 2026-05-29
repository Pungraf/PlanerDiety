using System.Globalization;
using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using DietPlanner.Mobile.Navigation;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class HomeViewModelTests
{
    [Fact]
    public async Task LoadCurrentDay_ShouldCalculateDailyKcalAndProteinTotals()
    {
        var plansClient = new FakePlansApiClient(
            new CurrentPlanDto(
                Guid.NewGuid(),
                "active",
                "2026-05-26",
                [
                    new PlanDayDto(
                        "2026-05-26",
                        [
                            new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Oats Bowl", 500, 30),
                            new PlanMealSlotDto("lunch", Guid.NewGuid(), "Chicken Rice", 700, 45),
                            new PlanMealSlotDto("dinner", Guid.NewGuid(), "Salmon Potatoes", 600, 35)
                        ])
                ]));
        var viewModel = new HomeViewModel(plansClient, new RecordingNavigator(), new InMemoryMealSearchContextStore(), new InMemoryMealDetailsContextStore());

        await viewModel.LoadAsync();

        Assert.NotNull(viewModel.SelectedDay);
        Assert.Equal(1800, viewModel.SelectedDay!.TotalKcal);
        Assert.Equal(110, viewModel.SelectedDay.TotalProtein);
    }

    [Fact]
    public async Task LoadAsync_ShouldSelectTodayWhenTodayExistsInPlan()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = today.AddDays(-1);
        var plansClient = new FakePlansApiClient(
            CreatePlan(
                yesterday,
                [
                    new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Yesterday breakfast", 400, 20)
                ],
                today,
                [
                    new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Today breakfast", 500, 30)
                ]));
        var viewModel = new HomeViewModel(plansClient, new RecordingNavigator(), new InMemoryMealSearchContextStore(), new InMemoryMealDetailsContextStore());

        await viewModel.LoadAsync();

        Assert.NotNull(viewModel.SelectedDay);
        Assert.Equal(today, viewModel.SelectedDay!.Date);
    }

    [Fact]
    public async Task CopySelectedDayToTarget_ShouldCallApiAndRefreshDays()
    {
        var sourceDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-2);
        var targetDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        var plansClient = new FakePlansApiClient(
            CreatePlan(
                sourceDate,
                [
                    new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Oats Bowl", 500, 30),
                    new PlanMealSlotDto("lunch", Guid.NewGuid(), "Chicken Rice", 700, 45),
                    new PlanMealSlotDto("dinner", Guid.NewGuid(), "Salmon Potatoes", 600, 35)
                ],
                targetDate,
                [
                    new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Toast", 250, 10)
                ]));
        var viewModel = new HomeViewModel(plansClient, new RecordingNavigator(), new InMemoryMealSearchContextStore(), new InMemoryMealDetailsContextStore());

        await viewModel.LoadAsync();
        var targetDay = Assert.Single(viewModel.Days, day => day.Date == targetDate);

        await viewModel.CopyDayCommand.ExecuteAsync(targetDay);

        Assert.Equal(sourceDate, plansClient.LastCopiedSourceDate);
        Assert.Equal(targetDate, plansClient.LastCopiedTargetDate);
        targetDay = Assert.Single(viewModel.Days, day => day.Date == targetDate);
        Assert.Equal(1800, targetDay.TotalKcal);
        Assert.Equal(110, targetDay.TotalProtein);
    }

    [Fact]
    public async Task OpenMealDetailsCommand_ShouldStoreMealIdAndNavigateToDetails()
    {
        var mealId = Guid.NewGuid();
        var plansClient = new FakePlansApiClient(
            new CurrentPlanDto(
                Guid.NewGuid(),
                "active",
                "2026-05-26",
                [
                    new PlanDayDto(
                        "2026-05-26",
                        [
                            new PlanMealSlotDto("breakfast", mealId, "Oats Bowl", 500, 30)
                        ])
                ]));
        var navigator = new RecordingNavigator();
        var mealDetailsStore = new InMemoryMealDetailsContextStore();
        var viewModel = new HomeViewModel(plansClient, navigator, new InMemoryMealSearchContextStore(), mealDetailsStore);

        await viewModel.LoadAsync();
        await viewModel.OpenMealDetailsCommand.ExecuteAsync(viewModel.SelectedDay!.Meals.First());

        Assert.NotNull(mealDetailsStore.Current);
        Assert.Equal(mealId, mealDetailsStore.Current!.MealId);
        Assert.Equal("meal-details", navigator.LastRoute);
    }

    [Fact]
    public async Task OpenMealDetailsCommand_ShouldNotNavigate_WhenMealIdIsMissing()
    {
        var plansClient = new FakePlansApiClient(
            new CurrentPlanDto(
                Guid.NewGuid(),
                "active",
                "2026-05-26",
                [
                    new PlanDayDto(
                        "2026-05-26",
                        [
                            new PlanMealSlotDto("breakfast", null, "Choose meal", 0, 0)
                        ])
                ]));
        var navigator = new RecordingNavigator();
        var mealDetailsStore = new InMemoryMealDetailsContextStore();
        var viewModel = new HomeViewModel(plansClient, navigator, new InMemoryMealSearchContextStore(), mealDetailsStore);

        await viewModel.LoadAsync();
        await viewModel.OpenMealDetailsCommand.ExecuteAsync(viewModel.SelectedDay!.Meals.First());

        Assert.Null(navigator.LastRoute);
        Assert.Null(mealDetailsStore.Current);
        Assert.Equal("Recipe details are not available for this meal.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task OpenMealDetailsCommand_ShouldNotCrash_WhenNavigationThrows()
    {
        var mealId = Guid.NewGuid();
        var plansClient = new FakePlansApiClient(
            new CurrentPlanDto(
                Guid.NewGuid(),
                "active",
                "2026-05-26",
                [
                    new PlanDayDto(
                        "2026-05-26",
                        [
                            new PlanMealSlotDto("breakfast", mealId, "Oats Bowl", 500, 30)
                        ])
                ]));
        var navigator = new ThrowingNavigator(new InvalidOperationException("Navigation failed."));
        var mealDetailsStore = new InMemoryMealDetailsContextStore();
        var viewModel = new HomeViewModel(plansClient, navigator, new InMemoryMealSearchContextStore(), mealDetailsStore);

        await viewModel.LoadAsync();

        var exception = await Record.ExceptionAsync(
            async () => await viewModel.OpenMealDetailsCommand.ExecuteAsync(viewModel.SelectedDay!.Meals.First()));

        Assert.Null(exception);
        Assert.Null(mealDetailsStore.Current);
        Assert.Equal("Could not open recipe details.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_ShouldRejectNonIsoApiDates()
    {
        using var cultureScope = new CultureScope(new CultureInfo("pl-PL"));
        var plansClient = new FakePlansApiClient(
            new CurrentPlanDto(
                Guid.NewGuid(),
                "active",
                "26.05.2026",
                [
                    new PlanDayDto(
                        "26.05.2026",
                        [
                            new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Oats Bowl", 500, 30)
                        ])
                ]));
        var viewModel = new HomeViewModel(plansClient, new RecordingNavigator(), new InMemoryMealSearchContextStore(), new InMemoryMealDetailsContextStore());

        await viewModel.LoadAsync();

        Assert.Equal("The API returned an invalid plan date.", viewModel.ErrorMessage);
        Assert.Null(viewModel.SelectedDay);
        Assert.Empty(viewModel.Days);
    }

    [Fact]
    public async Task LoadAsync_ShouldResetIsLoadingAfterSuccess()
    {
        var plansClient = new FakePlansApiClient(
            new CurrentPlanDto(
                Guid.NewGuid(),
                "active",
                DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
                [
                    new PlanDayDto(
                        DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
                        [new PlanMealSlotDto("breakfast", Guid.NewGuid(), "Oats Bowl", 500, 30)])
                ]));
        var viewModel = new HomeViewModel(plansClient, new RecordingNavigator(), new InMemoryMealSearchContextStore(), new InMemoryMealDetailsContextStore());

        await viewModel.LoadAsync();

        Assert.False(viewModel.IsLoading);
    }

    private static CurrentPlanDto CreatePlan(
        DateOnly sourceDate,
        IReadOnlyList<PlanMealSlotDto> sourceMeals,
        DateOnly targetDate,
        IReadOnlyList<PlanMealSlotDto> targetMeals)
    {
        return new CurrentPlanDto(
            Guid.NewGuid(),
            "active",
            sourceDate.ToString("yyyy-MM-dd"),
            [
                new PlanDayDto(sourceDate.ToString("yyyy-MM-dd"), sourceMeals),
                new PlanDayDto(targetDate.ToString("yyyy-MM-dd"), targetMeals)
            ]);
    }

    private sealed class FakePlansApiClient : IPlansApiClient
    {
        private CurrentPlanDto _plan;

        public FakePlansApiClient(CurrentPlanDto plan)
        {
            _plan = plan;
        }

        public DateOnly? LastCopiedSourceDate { get; private set; }

        public DateOnly? LastCopiedTargetDate { get; private set; }

        public bool? LastDeleteLinkedShoppingLists { get; private set; }

        public Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
        {
            LastCopiedSourceDate = sourceDate;
            LastCopiedTargetDate = targetDate;
            LastDeleteLinkedShoppingLists = deleteLinkedShoppingLists;

            var sourceDay = _plan.Days.Single(day => day.Date == sourceDate.ToString("yyyy-MM-dd"));
            _plan = _plan with
            {
                Days = _plan.Days
                    .Select(day => day.Date == targetDate.ToString("yyyy-MM-dd")
                        ? day with { Meals = sourceDay.Meals.Select(meal => meal with { }).ToArray() }
                        : day)
                    .ToArray()
            };

            return Task.CompletedTask;
        }

        public Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_plan);
        }

        public Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<MealSummaryDto>> GetMealCatalogAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<MealDetailsDto> GetMealDetailsAsync(Guid mealId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
            throw _exception;
        }
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUiCulture;

        public CultureScope(CultureInfo culture)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
