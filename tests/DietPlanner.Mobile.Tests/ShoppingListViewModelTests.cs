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

    [Fact]
    public async Task LoadAsync_ShouldAllowEmptyShoppingList()
    {
        var apiClient = new FakeShoppingListApiClient(new ShoppingListDto(Guid.Empty, []), new ShoppingListDto(Guid.Empty, []));
        var viewModel = new ShoppingListViewModel(apiClient);

        await viewModel.LoadAsync();

        Assert.Empty(viewModel.SummaryItems);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_ShouldIgnoreStaleResponse_WhenNewerRequestCompletesFirst()
    {
        var staleList = new ShoppingListDto(
            Guid.NewGuid(),
            [new ShoppingListSummaryItemDto(Guid.NewGuid(), "Stale", 1m, "pc", false)]);
        var freshList = new ShoppingListDto(
            Guid.NewGuid(),
            [new ShoppingListSummaryItemDto(Guid.NewGuid(), "Fresh", 2m, "pc", false)]);
        var apiClient = new SequencedShoppingListApiClient(staleList, freshList);
        var viewModel = new ShoppingListViewModel(apiClient);

        var firstLoad = viewModel.LoadAsync();
        var secondLoad = viewModel.LoadAsync();

        apiClient.ReleaseSecond();
        await secondLoad;
        apiClient.ReleaseFirst();
        await firstLoad;

        Assert.Single(viewModel.SummaryItems);
        Assert.Equal("Fresh", viewModel.SummaryItems[0].Name);
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

    private sealed class SequencedShoppingListApiClient : IShoppingListApiClient
    {
        private readonly ShoppingListDto _firstResponse;
        private readonly ShoppingListDto _secondResponse;
        private readonly TaskCompletionSource _firstGate = new();
        private readonly TaskCompletionSource _secondGate = new();
        private int _calls;

        public SequencedShoppingListApiClient(ShoppingListDto firstResponse, ShoppingListDto secondResponse)
        {
            _firstResponse = firstResponse;
            _secondResponse = secondResponse;
        }

        public Task<ShoppingListDto> ToggleItemAsync(Guid itemId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async Task<ShoppingListDto> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            var call = Interlocked.Increment(ref _calls);
            if (call == 1)
            {
                await _firstGate.Task.WaitAsync(cancellationToken);
                return _firstResponse;
            }

            await _secondGate.Task.WaitAsync(cancellationToken);
            return _secondResponse;
        }

        public void ReleaseFirst() => _firstGate.TrySetResult();

        public void ReleaseSecond() => _secondGate.TrySetResult();
    }
}
