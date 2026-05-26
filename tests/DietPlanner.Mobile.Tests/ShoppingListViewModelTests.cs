using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class ShoppingListViewModelTests
{
    [Fact]
    public async Task ToggleItem_ShouldRefreshListWithCheckedItemAtBottom()
    {
        var milkId = Guid.NewGuid();
        var oatsId = Guid.NewGuid();
        var apiClient = new FakeShoppingListApiClient(
            new ShoppingListDto(
                Guid.NewGuid(),
                [
                    new ShoppingListSummaryItemDto(milkId, "Milk", 2m, "l", false),
                    new ShoppingListSummaryItemDto(oatsId, "Oats", 1m, "kg", false)
                ]),
            new ShoppingListDto(
                Guid.NewGuid(),
                [
                    new ShoppingListSummaryItemDto(oatsId, "Oats", 1m, "kg", false),
                    new ShoppingListSummaryItemDto(milkId, "Milk", 2m, "l", true)
                ]));
        var viewModel = new ShoppingListViewModel(apiClient);

        await viewModel.LoadAsync();
        await viewModel.ToggleItemAsync(viewModel.SummaryItems.First());

        var toggledItem = viewModel.SummaryItems.Last();
        Assert.True(toggledItem.IsChecked);
        Assert.Equal("Milk", toggledItem.Name);
    }

    private sealed class FakeShoppingListApiClient : IShoppingListApiClient
    {
        private readonly ShoppingListDto _initialList;
        private readonly ShoppingListDto _toggledList;

        public FakeShoppingListApiClient(ShoppingListDto initialList, ShoppingListDto toggledList)
        {
            _initialList = initialList;
            _toggledList = toggledList;
        }

        public Task<ShoppingListDto> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_initialList);
        }

        public Task<ShoppingListDto> ToggleItemAsync(Guid itemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_toggledList);
        }
    }
}
