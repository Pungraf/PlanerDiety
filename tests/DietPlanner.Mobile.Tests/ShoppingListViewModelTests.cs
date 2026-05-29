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
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), new RecordingPromptService(confirmResult: false));

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
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), new RecordingPromptService(confirmResult: false));

        await viewModel.LoadAsync();
        await viewModel.SelectListCommand.ExecuteAsync(viewModel.Lists.Single());

        Assert.False(viewModel.ShowListPicker);
        Assert.True(viewModel.ShowListDetails);
        Assert.Single(viewModel.Items);
        Assert.Equal("Milk", viewModel.Items[0].Name);
    }

    [Fact]
    public async Task DeleteListFromIndex_ShouldRemoveListAndStayOnIndex()
    {
        var firstListId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondListId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var api = new FakeShoppingListApiClient(
            lists:
            [
                new ShoppingListSummaryDto(firstListId, "Week groceries", "2026-05-29", 4),
                new ShoppingListSummaryDto(secondListId, "Dinner only", "2026-05-29", 2)
            ]);
        var prompts = new RecordingPromptService(confirmResult: true);
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), prompts);

        await viewModel.LoadAsync();
        await viewModel.DeleteListAsync(viewModel.Lists.First());

        var remaining = Assert.Single(viewModel.Lists);
        Assert.Equal(secondListId, remaining.Id);
        Assert.Null(viewModel.SelectedList);
        Assert.Empty(viewModel.Items);
        Assert.True(viewModel.ShowListPicker);
        Assert.Equal([firstListId], api.DeletedListIds);
        Assert.Equal(2, api.ListCalls);
        Assert.Equal(1, prompts.ConfirmCalls);
    }

    [Fact]
    public async Task DeleteSelectedList_ShouldClearSelectionAndRefreshIndex()
    {
        var listId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var api = new FakeShoppingListApiClient(
            lists:
            [
                new ShoppingListSummaryDto(listId, "Week groceries", "2026-05-29", 4)
            ],
            details: new ShoppingListDetailsDto(
                listId,
                "Week groceries",
                [
                    new ShoppingListSummaryItemDto(Guid.NewGuid(), "Milk", 2m, "l", false)
                ]));
        var prompts = new RecordingPromptService(confirmResult: true);
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), prompts);

        await viewModel.LoadAsync();
        await viewModel.SelectListAsync(viewModel.Lists.Single());
        await viewModel.DeleteSelectedListAsync();

        Assert.Null(viewModel.SelectedList);
        Assert.Empty(viewModel.Lists);
        Assert.Empty(viewModel.Items);
        Assert.True(viewModel.ShowListPicker);
        Assert.False(viewModel.ShowListDetails);
        Assert.Equal([listId], api.DeletedListIds);
        Assert.Equal(2, api.ListCalls);
        Assert.Equal(1, prompts.ConfirmCalls);
    }

    [Fact]
    public async Task BackToLists_ShouldClearSelectionAndReturnToIndex()
    {
        var listId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var api = new FakeShoppingListApiClient(
            lists:
            [
                new ShoppingListSummaryDto(listId, "Week groceries", "2026-05-29", 4)
            ],
            details: new ShoppingListDetailsDto(
                listId,
                "Week groceries",
                [
                    new ShoppingListSummaryItemDto(Guid.NewGuid(), "Milk", 2m, "l", false)
                ]));
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), new RecordingPromptService(confirmResult: false));

        await viewModel.LoadAsync();
        await viewModel.SelectListAsync(viewModel.Lists.Single());

        await viewModel.BackToListsAsync();

        Assert.Null(viewModel.SelectedList);
        Assert.Empty(viewModel.Items);
        Assert.True(viewModel.ShowListPicker);
        Assert.False(viewModel.ShowListDetails);
    }

    [Fact]
    public async Task ToggleItem_ShouldPreserveSelectedDetailsState()
    {
        var listId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var api = new FakeShoppingListApiClient(
            lists:
            [
                new ShoppingListSummaryDto(listId, "Week groceries", "2026-05-29", 1)
            ],
            details: new ShoppingListDetailsDto(
                listId,
                "Week groceries",
                [
                    new ShoppingListSummaryItemDto(itemId, "Milk", 2m, "l", false)
                ]),
            toggledDetails: new ShoppingListDetailsDto(
                listId,
                "Week groceries",
                [
                    new ShoppingListSummaryItemDto(itemId, "Milk", 2m, "l", true)
                ]));
        var viewModel = new ShoppingListViewModel(api, new RecordingNavigator(), new RecordingPromptService(confirmResult: false));

        await viewModel.LoadAsync();
        var selectedList = viewModel.Lists.Single();
        await viewModel.SelectListAsync(selectedList);

        await viewModel.ToggleItemAsync(viewModel.Items.Single());

        Assert.Same(selectedList, viewModel.SelectedList);
        Assert.True(viewModel.ShowListDetails);
        Assert.False(viewModel.ShowListPicker);
        var item = Assert.Single(viewModel.Items);
        Assert.True(item.IsChecked);
        Assert.Equal([itemId], api.ToggledItemIds);
    }

    [Fact]
    public void ShoppingListPage_ShouldBindIndexDetailsCommands()
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
            "ShoppingListPage.xaml"));

        var xaml = File.ReadAllText(pageXamlPath);

        Assert.Contains("BackToListsCommand", xaml);
        Assert.Contains("DeleteListCommand", xaml);
        Assert.Contains("DeleteSelectedListCommand", xaml);
        Assert.Contains("ToggleItemCommand", xaml);
    }

    private sealed class FakeShoppingListApiClient : IShoppingListApiClient
    {
        private readonly List<ShoppingListSummaryDto> _lists;
        private readonly ShoppingListDetailsDto _details;
        private readonly ShoppingListDetailsDto _toggledDetails;

        public FakeShoppingListApiClient(
            IReadOnlyList<ShoppingListSummaryDto>? lists = null,
            ShoppingListDetailsDto? details = null,
            ShoppingListDetailsDto? toggledDetails = null)
        {
            _lists = lists?.ToList() ?? [];
            _details = details ?? new ShoppingListDetailsDto(Guid.Empty, string.Empty, []);
            _toggledDetails = toggledDetails ?? _details;
        }

        public List<Guid> DeletedListIds { get; } = [];

        public List<Guid> ToggledItemIds { get; } = [];

        public int ListCalls { get; private set; }

        public Task<ShoppingListDetailsDto> CreateAsync(string name, IReadOnlyList<CreateShoppingListIngredientRequest> ingredientKeys, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Guid listId, CancellationToken cancellationToken = default)
        {
            DeletedListIds.Add(listId);
            _lists.RemoveAll(list => list.Id == listId);
            return Task.CompletedTask;
        }

        public Task<ShoppingListDetailsDto> GetDetailsAsync(Guid listId, CancellationToken cancellationToken = default)
            => Task.FromResult(_details);

        public Task<IReadOnlyList<ShoppingListCreateDayOptionDto>> GetCreateOptionsAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<ShoppingListSummaryDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyList<ShoppingListSummaryDto>>(_lists.ToArray());
        }

        public Task<ShoppingListDetailsDto> ToggleItemAsync(Guid listId, Guid itemId, CancellationToken cancellationToken = default)
        {
            ToggledItemIds.Add(itemId);
            return Task.FromResult(_toggledDetails);
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

    private sealed class RecordingPromptService : IUserPromptService
    {
        private readonly bool _confirmResult;

        public RecordingPromptService(bool confirmResult)
        {
            _confirmResult = confirmResult;
        }

        public int ConfirmCalls { get; private set; }

        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        {
            ConfirmCalls++;
            return Task.FromResult(_confirmResult);
        }
    }
}
