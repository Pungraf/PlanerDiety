using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class MealDetailsViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldLoadMealDetailsFromContext()
    {
        var mealId = Guid.NewGuid();
        var context = new InMemoryMealDetailsContextStore
        {
            Current = new MealDetailsContext(mealId)
        };
        var api = new FakePlansApiClient(new MealDetailsDto(mealId, "Owsianka", "breakfast", 450, 25, "1. Gotuj. 2. Mieszaj.", []));
        var viewModel = new MealDetailsViewModel(api, context);

        await viewModel.LoadAsync();

        Assert.Equal("Owsianka", viewModel.Name);
        Assert.Equal("1. Gotuj. 2. Mieszaj.", viewModel.Description);
        Assert.Equal(["1. Gotuj.", "2. Mieszaj."], viewModel.PreparationSteps.Select(step => step.Text));
    }

    [Fact]
    public async Task LoadAsync_ShouldSetError_WhenNoMealIsSelected()
    {
        var context = new InMemoryMealDetailsContextStore();
        var api = new FakePlansApiClient(new MealDetailsDto(Guid.NewGuid(), "Owsianka", "breakfast", 450, 25, "Gotuj.", []));
        var viewModel = new MealDetailsViewModel(api, context);

        await viewModel.LoadAsync();

        Assert.Equal("Could not load recipe details.", viewModel.ErrorMessage);
        Assert.Equal(string.Empty, viewModel.Name);
        Assert.Empty(viewModel.Ingredients);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetError_WhenApiThrows()
    {
        var mealId = Guid.NewGuid();
        var context = new InMemoryMealDetailsContextStore
        {
            Current = new MealDetailsContext(mealId)
        };
        var api = new ThrowingPlansApiClient(new HttpRequestException("502"));
        var viewModel = new MealDetailsViewModel(api, context);

        var exception = await Record.ExceptionAsync(async () => await viewModel.LoadAsync());

        Assert.Null(exception);
        Assert.Equal("Could not load recipe details.", viewModel.ErrorMessage);
        Assert.Empty(viewModel.Ingredients);
    }

    [Fact]
    public async Task LoadAsync_ShouldToggleLoadingState_AndPopulateRecipeCard()
    {
        var mealId = Guid.NewGuid();
        var context = new InMemoryMealDetailsContextStore
        {
            Current = new MealDetailsContext(mealId)
        };
        var api = new FakePlansApiClient(
            new MealDetailsDto(
                mealId,
                "Owsianka",
                "breakfast",
                450,
                25,
                "Gotuj i mieszaj.",
                [new MealDetailsIngredientDto("Platki", 80m, "g", "Pantry")]));
        var viewModel = new MealDetailsViewModel(api, context);

        await viewModel.LoadAsync();

        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
        Assert.Equal("Owsianka", viewModel.Name);
        Assert.Equal("Gotuj i mieszaj.", viewModel.Description);
        Assert.Single(viewModel.Ingredients);
    }

    [Fact]
    public void SplitPreparationSteps_ShouldSplitNumberedStepsIntoSeparateItems()
    {
        var steps = MealDetailsViewModel.SplitPreparationSteps("1.\nPodsmaz cebule 2. Dodaj pomidory\n3. Gotuj 10 minut");

        Assert.Equal(
            ["1. Podsmaz cebule", "2. Dodaj pomidory", "3. Gotuj 10 minut"],
            steps);
    }

    [Fact]
    public void MealDetailsPage_ShouldBindPreparationSteps()
    {
        var pageXamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "DietPlanner.Mobile",
            "Views",
            "MealDetailsPage.xaml"));

        var xaml = File.ReadAllText(pageXamlPath);

        Assert.Contains("PreparationSteps", xaml);
        Assert.Contains("BindableLayout.ItemsSource", xaml);
    }

    private sealed class FakePlansApiClient : IPlansApiClient
    {
        private readonly MealDetailsDto _details;

        public FakePlansApiClient(MealDetailsDto details)
        {
            _details = details;
        }

        public Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CopyDayAsync(Guid planId, DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlanningStateDto> GetPlanningStateAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MealSummaryDto>> GetMealCatalogAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MealDetailsDto> GetMealDetailsAsync(Guid mealId, CancellationToken cancellationToken = default)
            => Task.FromResult(_details);

        public Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ReplaceMealAsync(Guid planId, DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlanningStateDto> GenerateFutureWeekAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingPlansApiClient : IPlansApiClient
    {
        private readonly Exception _exception;

        public ThrowingPlansApiClient(Exception exception)
        {
            _exception = exception;
        }

        public Task CopyDayAsync(DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task CopyDayAsync(Guid planId, DateOnly sourceDate, DateOnly targetDate, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CurrentPlanDto> GetCurrentPlanAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlanningStateDto> GetPlanningStateAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MealSummaryDto>> SearchMealsAsync(string? query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MealSummaryDto>> GetMealCatalogAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MealDetailsDto> GetMealDetailsAsync(Guid mealId, CancellationToken cancellationToken = default)
            => Task.FromException<MealDetailsDto>(_exception);

        public Task ReplaceMealAsync(DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ReplaceMealAsync(Guid planId, DateOnly date, string slotType, Guid mealId, bool deleteLinkedShoppingLists, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PlanningStateDto> GenerateFutureWeekAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
