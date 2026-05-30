using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class ShoppingListViewModel : INotifyPropertyChanged
{
    private readonly IShoppingListApiClient _shoppingListApiClient;
    private readonly IAppNavigator _navigator;
    private readonly IUserPromptService _promptService;
    private readonly ISelectedPlanContextStore _selectedPlanContextStore;
    private string? _errorMessage;
    private int _requestVersion;
    private bool _isLoading;
    private ShoppingListSummaryViewModel? _selectedList;

    public ShoppingListViewModel(
        IShoppingListApiClient shoppingListApiClient,
        IAppNavigator navigator,
        IUserPromptService promptService,
        ISelectedPlanContextStore selectedPlanContextStore)
    {
        _shoppingListApiClient = shoppingListApiClient;
        _navigator = navigator;
        _promptService = promptService;
        _selectedPlanContextStore = selectedPlanContextStore;
        LoadCommand = new AsyncCommand(_ => LoadAsync());
        ToggleItemCommand = new AsyncCommand(item => ToggleItemAsync(item as ShoppingListSummaryItemViewModel));
        SelectListCommand = new AsyncCommand(item => SelectListAsync(item as ShoppingListSummaryViewModel));
        DeleteListCommand = new AsyncCommand(item => DeleteListAsync(item as ShoppingListSummaryViewModel));
        DeleteSelectedListCommand = new AsyncCommand(_ => DeleteSelectedListAsync());
        BackToListsCommand = new AsyncCommand(_ => BackToListsAsync());
        OpenCreateCommand = new AsyncCommand(_ => _navigator.GoToAsync("shopping-list-create"));
        GoToHomeCommand = new AsyncCommand(_ => _navigator.GoToMainTabAsync(MainAppTab.Home));
        GoToShoppingListsCommand = new AsyncCommand(_ => _navigator.GoToMainTabAsync(MainAppTab.ShoppingList));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ShoppingListSummaryViewModel> Lists { get; } = [];

    public ObservableCollection<ShoppingListSummaryItemViewModel> Items { get; } = [];

    public AsyncCommand LoadCommand { get; }

    public AsyncCommand ToggleItemCommand { get; }

    public AsyncCommand SelectListCommand { get; }

    public AsyncCommand DeleteListCommand { get; }

    public AsyncCommand DeleteSelectedListCommand { get; }

    public AsyncCommand BackToListsCommand { get; }

    public AsyncCommand OpenCreateCommand { get; }

    public AsyncCommand GoToHomeCommand { get; }

    public AsyncCommand GoToShoppingListsCommand { get; }

    public bool ShowListPicker => SelectedList is null;

    public bool ShowListDetails => SelectedList is not null;

    public ShoppingListSummaryViewModel? SelectedList
    {
        get => _selectedList;
        private set
        {
            if (_selectedList == value)
            {
                return;
            }

            _selectedList = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowListPicker));
            OnPropertyChanged(nameof(ShowListDetails));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowList));
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool ShowList => !IsLoading;

    public bool ShowEmptyState => !IsLoading && Lists.Count == 0 && Items.Count == 0 && string.IsNullOrWhiteSpace(ErrorMessage);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var requestVersion = Interlocked.Increment(ref _requestVersion);
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var shoppingLists = await _shoppingListApiClient.ListAsync(_selectedPlanContextStore.SelectedPlanId, cancellationToken);
            if (requestVersion == _requestVersion)
            {
                ApplyLists(shoppingLists);
            }
        }
        catch (Exception exception)
        {
            if (requestVersion == _requestVersion)
            {
                ErrorMessage = exception.Message;
            }
        }
        finally
        {
            if (requestVersion == _requestVersion)
            {
                IsLoading = false;
            }
        }
    }

    public async Task SelectListAsync(ShoppingListSummaryViewModel? list, CancellationToken cancellationToken = default)
    {
        if (list is null)
        {
            return;
        }

        var requestVersion = Interlocked.Increment(ref _requestVersion);
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var shoppingList = await _shoppingListApiClient.GetDetailsAsync(list.Id, _selectedPlanContextStore.SelectedPlanId, cancellationToken);
            if (requestVersion == _requestVersion)
            {
                SelectedList = list;
                ApplyItems(shoppingList);
            }
        }
        catch (Exception exception)
        {
            if (requestVersion == _requestVersion)
            {
                ErrorMessage = exception.Message;
            }
        }
        finally
        {
            if (requestVersion == _requestVersion)
            {
                IsLoading = false;
            }
        }
    }

    public async Task ToggleItemAsync(ShoppingListSummaryItemViewModel? item, CancellationToken cancellationToken = default)
    {
        if (item is null || SelectedList is null)
        {
            return;
        }

        var requestVersion = Interlocked.Increment(ref _requestVersion);
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var shoppingList = await _shoppingListApiClient.ToggleItemAsync(SelectedList.Id, item.Id, cancellationToken);
            if (requestVersion == _requestVersion)
            {
                ApplyItems(shoppingList);
            }
        }
        catch (Exception exception)
        {
            if (requestVersion == _requestVersion)
            {
                ErrorMessage = exception.Message;
            }
        }
        finally
        {
            if (requestVersion == _requestVersion)
            {
                IsLoading = false;
            }
        }
    }

    public async Task DeleteListAsync(ShoppingListSummaryViewModel? list, CancellationToken cancellationToken = default)
    {
        if (list is null)
        {
            return;
        }

        var confirmed = await ConfirmDeleteAsync();
        if (!confirmed)
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            await _shoppingListApiClient.DeleteAsync(list.Id, cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    public Task BackToListsAsync(CancellationToken cancellationToken = default)
    {
        ClearSelection();
        return Task.CompletedTask;
    }

    public Task DeleteSelectedListAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedList is null)
        {
            return Task.CompletedTask;
        }

        return DeleteSelectedListAsync(SelectedList, cancellationToken);
    }

    private void ApplyLists(IReadOnlyList<ShoppingListSummaryDto> shoppingLists)
    {
        Lists.Clear();
        foreach (var list in shoppingLists.Select(list => new ShoppingListSummaryViewModel(list.Id, list.Name, list.CreatedAt, list.ItemCount)))
        {
            Lists.Add(list);
        }

        SelectedList = null;
        Items.Clear();
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private void ApplyItems(ShoppingListDetailsDto shoppingList)
    {
        var orderedItems = (shoppingList.Items ?? [])
            .OrderBy(item => item.IsChecked)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        var mappedItems = orderedItems
            .Select((item, index) => new ShoppingListSummaryItemViewModel(
                item.Id,
                item.Name,
                item.Quantity,
                item.Unit,
                item.IsChecked,
                item.IsChecked && (index == 0 || !orderedItems[index - 1].IsChecked)))
            .ToArray();

        Items.Clear();
        foreach (var item in mappedItems)
        {
            Items.Add(item);
        }

        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private async Task DeleteSelectedListAsync(ShoppingListSummaryViewModel list, CancellationToken cancellationToken)
    {
        var confirmed = await ConfirmDeleteAsync();
        if (!confirmed)
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            await _shoppingListApiClient.DeleteAsync(list.Id, cancellationToken);
            ClearSelection();
            await LoadAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private Task<bool> ConfirmDeleteAsync()
        => _promptService.ConfirmAsync(
            "Delete shopping list?",
            "This shopping list will be removed.",
            "Delete",
            "Cancel");

    private void ClearSelection()
    {
        SelectedList = null;
        Items.Clear();
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ShoppingListSummaryViewModel
{
    public ShoppingListSummaryViewModel(Guid id, string name, string createdAt, int itemCount)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        ItemCount = itemCount;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string CreatedAt { get; }

    public int ItemCount { get; }
}

public sealed class ShoppingListSummaryItemViewModel
{
    public ShoppingListSummaryItemViewModel(Guid id, string name, decimal quantity, string unit, bool isChecked, bool showCheckedDivider)
    {
        Id = id;
        Name = name;
        Quantity = quantity;
        Unit = unit;
        IsChecked = isChecked;
        ShowCheckedDivider = showCheckedDivider;
    }

    public Guid Id { get; }

    public string Name { get; }

    public decimal Quantity { get; }

    public string Unit { get; }

    public bool IsChecked { get; }

    public bool ShowCheckedDivider { get; }

    public string QuantityText => $"{Quantity.ToString("0.##", CultureInfo.InvariantCulture)} {Unit}".Trim();

    public string ActionLabel => IsChecked ? "Undo" : "Check";
}
