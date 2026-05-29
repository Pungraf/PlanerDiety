using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class ShoppingListCreateViewModelTests
{
    [Fact]
    public async Task ToggleMeal_ShouldToggleAllIngredientsInMeal()
    {
        var meal = new ShoppingListCreateMealOptionDto(
            "breakfast",
            "Breakfast",
            [
                new ShoppingListCreateIngredientOptionDto(Guid.NewGuid(), "Oats", 80m, "g", "Pantry", true)
            ]);
        var api = new FakeShoppingListApiClient(
            createOptions:
            [
                new ShoppingListCreateDayOptionDto("2026-05-25", [meal])
            ]);
        var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

        await viewModel.LoadAsync();
        viewModel.ToggleMealCommand.Execute(viewModel.Days.Single().Meals.Single());

        Assert.False(viewModel.Days.Single().Meals.Single().Ingredients.Single().IsSelected);
    }

    [Fact]
    public async Task SelectFullWeekPreset_ShouldPreselectAllIngredients()
    {
        var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
        var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

        await viewModel.LoadAsync();
        viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.FullWeek);

        Assert.False(viewModel.IsPresetStep);
        Assert.True(viewModel.IsBuilderStep);
        Assert.All(
            viewModel.Days.SelectMany(day => day.Meals).SelectMany(meal => meal.Ingredients),
            ingredient => Assert.True(ingredient.IsSelected));
    }

    [Fact]
    public async Task SelectSelectedDaysPreset_ShouldShowDaySelectionStep()
    {
        var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
        var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

        await viewModel.LoadAsync();
        viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.SelectedDays);

        Assert.False(viewModel.IsPresetStep);
        Assert.True(viewModel.IsDaySelectionStep);
        Assert.False(viewModel.IsBuilderStep);
    }

    [Fact]
    public async Task ContinueSelectedDays_ShouldPreselectOnlySelectedDaysAndShowBuilder()
    {
        var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
        var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

        await viewModel.LoadAsync();
        viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.SelectedDays);
        viewModel.Days.First().IsSelected = true;

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsPresetStep);
        Assert.False(viewModel.IsDaySelectionStep);
        Assert.True(viewModel.IsBuilderStep);
        Assert.All(viewModel.Days.First().Meals.SelectMany(meal => meal.Ingredients), ingredient => Assert.True(ingredient.IsSelected));
        Assert.All(viewModel.Days.Skip(1).SelectMany(day => day.Meals).SelectMany(meal => meal.Ingredients), ingredient => Assert.False(ingredient.IsSelected));
    }

    [Fact]
    public async Task ContinueSelectedDays_ShouldRequireAtLeastOneDay()
    {
        var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
        var viewModel = new ShoppingListCreateViewModel(api, new RecordingNavigator());

        await viewModel.LoadAsync();
        viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.SelectedDays);

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsDaySelectionStep);
        Assert.Equal("Select at least one day.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_ShouldSendSelectedIngredientsAndNavigateBackToIndex()
    {
        var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
        var navigator = new RecordingNavigator();
        var viewModel = new ShoppingListCreateViewModel(api, navigator);

        await viewModel.LoadAsync();
        viewModel.SelectPresetCommand.Execute(ShoppingListCreatePreset.FullWeek);
        await viewModel.SaveAsync();

        Assert.Equal("//main/shopping-list", navigator.LastRoute);
        Assert.Equal(
            [
                new CreateShoppingListIngredientRequest("2026-05-25", "breakfast", Guid.Parse("11111111-1111-1111-1111-111111111111")),
                new CreateShoppingListIngredientRequest("2026-05-25", "lunch", Guid.Parse("22222222-2222-2222-2222-222222222222")),
                new CreateShoppingListIngredientRequest("2026-05-26", "dinner", Guid.Parse("33333333-3333-3333-3333-333333333333"))
            ],
            api.CreatedIngredientKeys);
    }

    [Fact]
    public async Task SaveAsync_ShouldSurfaceErrorMessageAndAvoidNavigation_WhenCreateFails()
    {
        var api = new FakeShoppingListApiClient(
            createOptions: BuildCreateOptions(),
            createException: new InvalidOperationException("Could not create shopping list."));
        var navigator = new RecordingNavigator();
        var viewModel = new ShoppingListCreateViewModel(api, navigator);

        await viewModel.LoadAsync();
        await viewModel.SelectPresetCommand.ExecuteAsync(ShoppingListCreatePreset.FullWeek);

        var exception = await Record.ExceptionAsync(() => viewModel.SaveCommand.ExecuteAsync(null));

        Assert.Null(exception);
        Assert.Equal("Could not create shopping list.", viewModel.ErrorMessage);
        Assert.Null(navigator.LastRoute);
    }

    [Fact]
    public async Task SaveAsync_ShouldNotCreateDuplicate_WhenNavigationFailsAfterCreate()
    {
        var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
        var navigator = new RecordingNavigator(navigationException: new InvalidOperationException("Navigation failed."));
        var viewModel = new ShoppingListCreateViewModel(api, navigator);

        await viewModel.LoadAsync();
        await viewModel.SelectPresetCommand.ExecuteAsync(ShoppingListCreatePreset.FullWeek);

        await viewModel.SaveAsync();
        await viewModel.SaveAsync();

        Assert.Equal(1, api.CreateCalls);
        Assert.Equal("Shopping list created, but navigation back to the shopping list failed.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_ShouldAllowCreateAgain_WhenSelectionChangesAfterNavigationFailure()
    {
        var api = new FakeShoppingListApiClient(createOptions: BuildCreateOptions());
        var navigator = new RecordingNavigator(navigationException: new InvalidOperationException("Navigation failed."));
        var viewModel = new ShoppingListCreateViewModel(api, navigator);

        await viewModel.LoadAsync();
        await viewModel.SelectPresetCommand.ExecuteAsync(ShoppingListCreatePreset.FullWeek);

        await viewModel.SaveAsync();
        viewModel.Days.First().Meals.First().Ingredients.Single().IsSelected = false;
        await viewModel.SaveAsync();

        Assert.Equal(2, api.CreateCalls);
    }

    [Fact]
    public void ShoppingListCreatePage_ShouldContainPresetAndSaveBindings()
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
            "ShoppingListCreatePage.xaml"));

        var xaml = File.ReadAllText(pageXamlPath);

        Assert.Contains("IsVisible=\"{Binding IsPresetStep}\"", xaml);
        Assert.Contains("SelectPresetCommand", xaml);
        Assert.Contains("IsVisible=\"{Binding IsDaySelectionStep}\"", xaml);
        Assert.Contains("ContinueCommand", xaml);
        Assert.Contains("IsVisible=\"{Binding IsBuilderStep}\"", xaml);
        Assert.Contains("SaveCommand", xaml);
    }

    private static IReadOnlyList<ShoppingListCreateDayOptionDto> BuildCreateOptions()
        =>
        [
            new ShoppingListCreateDayOptionDto(
                "2026-05-25",
                [
                    new ShoppingListCreateMealOptionDto(
                        "breakfast",
                        "Oats",
                        [
                            new ShoppingListCreateIngredientOptionDto(
                                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                                "Oats",
                                80m,
                                "g",
                                "Pantry",
                                false)
                        ]),
                    new ShoppingListCreateMealOptionDto(
                        "lunch",
                        "Chicken bowl",
                        [
                            new ShoppingListCreateIngredientOptionDto(
                                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                                "Chicken",
                                140m,
                                "g",
                                "Meat",
                                false)
                        ])
                ]),
            new ShoppingListCreateDayOptionDto(
                "2026-05-26",
                [
                    new ShoppingListCreateMealOptionDto(
                        "dinner",
                        "Tomato soup",
                        [
                            new ShoppingListCreateIngredientOptionDto(
                                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                                "Tomato",
                                180m,
                                "g",
                                "Produce",
                                false)
                        ])
                ])
        ];
}

file sealed class FakeShoppingListApiClient : IShoppingListApiClient
{
    private readonly IReadOnlyList<ShoppingListCreateDayOptionDto> _createOptions;
    private readonly Exception? _createException;

    public FakeShoppingListApiClient(IReadOnlyList<ShoppingListCreateDayOptionDto>? createOptions = null, Exception? createException = null)
    {
        _createOptions = createOptions ?? [];
        _createException = createException;
    }

    public IReadOnlyList<CreateShoppingListIngredientRequest> CreatedIngredientKeys { get; private set; } = [];

    public int CreateCalls { get; private set; }

    public Task<ShoppingListDetailsDto> CreateAsync(string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default)
    {
        CreateCalls++;

        if (_createException is not null)
        {
            throw _createException;
        }

        CreatedIngredientKeys = ingredientKeys.ToArray();
        return Task.FromResult(new ShoppingListDetailsDto(Guid.NewGuid(), name, []));
    }

    public Task DeleteAsync(Guid listId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<ShoppingListDetailsDto> GetDetailsAsync(Guid listId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<ShoppingListCreateDayOptionDto>> GetCreateOptionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_createOptions);

    public Task<IReadOnlyList<ShoppingListSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ShoppingListSummaryDto>>([]);

    public Task<ShoppingListDetailsDto> ToggleItemAsync(Guid listId, Guid itemId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

file sealed class RecordingNavigator : IAppNavigator
{
    private readonly Exception? _navigationException;

    public RecordingNavigator(Exception? navigationException = null)
    {
        _navigationException = navigationException;
    }

    public string? LastRoute { get; private set; }

    public Task GoToAsync(string route)
    {
        if (_navigationException is not null)
        {
            throw _navigationException;
        }

        LastRoute = route;
        return Task.CompletedTask;
    }
}
