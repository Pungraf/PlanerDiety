using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class ShoppingListViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldShowShoppingListsBeforeItems()
    {
        var api = new FakeShoppingListApiClient(
            lists:
            [
                new ShoppingListSummaryDto(Guid.NewGuid(), "Weekly shop", "2026-05-28T10:00:00Z", 3)
            ]);
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator());

        await viewModel.LoadAsync();

        Assert.Single(viewModel.Lists);
        Assert.Equal("Weekly shop", viewModel.Lists[0].Name);
        Assert.True(viewModel.ShowListPicker);
    }

    [Fact]
    public async Task SelectList_ShouldLoadItemsForSelectedList()
    {
        var listId = Guid.NewGuid();
        var api = new FakeShoppingListApiClient(
            lists:
            [
                new ShoppingListSummaryDto(listId, "Weekly shop", "2026-05-28T10:00:00Z", 2)
            ],
            details: new ShoppingListDetailsDto(
                listId,
                "Weekly shop",
                [
                    new ShoppingListSummaryItemDto(Guid.NewGuid(), "Milk", 2m, "l", false)
                ]));
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator());

        await viewModel.LoadAsync();
        await viewModel.SelectListCommand.ExecuteAsync(viewModel.Lists.Single());

        Assert.False(viewModel.ShowListPicker);
        Assert.True(viewModel.ShowListDetails);
        Assert.Single(viewModel.Items);
        Assert.Equal("Milk", viewModel.Items[0].Name);
    }

    private sealed class FakeShoppingListApiClient : IShoppingListApiClient
    {
        private readonly IReadOnlyList<ShoppingListSummaryDto> _lists;
        private readonly ShoppingListDetailsDto _details;

        public FakeShoppingListApiClient(
            IReadOnlyList<ShoppingListSummaryDto>? lists = null,
            ShoppingListDetailsDto? details = null)
        {
            _lists = lists ?? [];
            _details = details ?? new ShoppingListDetailsDto(Guid.Empty, string.Empty, []);
        }

        public Task<ShoppingListDetailsDto> CreateAsync(string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Guid listId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ShoppingListDetailsDto> GetDetailsAsync(Guid listId, CancellationToken cancellationToken = default)
            => Task.FromResult(_details);

        public Task<IReadOnlyList<ShoppingListCreateDayOptionDto>> GetCreateOptionsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ShoppingListSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_lists);

        public Task<ShoppingListDetailsDto> ToggleItemAsync(Guid listId, Guid itemId, CancellationToken cancellationToken = default)
            => Task.FromResult(_details);
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
}
