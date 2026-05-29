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
}

file sealed class FakeShoppingListApiClient : IShoppingListApiClient
{
    private readonly IReadOnlyList<ShoppingListCreateDayOptionDto> _createOptions;

    public FakeShoppingListApiClient(IReadOnlyList<ShoppingListCreateDayOptionDto>? createOptions = null)
    {
        _createOptions = createOptions ?? [];
    }

    public Task<ShoppingListDetailsDto> CreateAsync(string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

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
    public Task GoToAsync(string route) => Task.CompletedTask;
}
