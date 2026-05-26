using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class ShoppingListViewModel : INotifyPropertyChanged
{
    private readonly IShoppingListApiClient _shoppingListApiClient;
    private string? _errorMessage;
    private int _requestVersion;

    public ShoppingListViewModel(IShoppingListApiClient shoppingListApiClient)
    {
        _shoppingListApiClient = shoppingListApiClient;
        LoadCommand = new AsyncCommand(_ => LoadAsync());
        ToggleItemCommand = new AsyncCommand(item => ToggleItemAsync(item as ShoppingListSummaryItemViewModel));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ShoppingListSummaryItemViewModel> SummaryItems { get; } = [];

    public AsyncCommand LoadCommand { get; }

    public AsyncCommand ToggleItemCommand { get; }

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

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var requestVersion = Interlocked.Increment(ref _requestVersion);
        ErrorMessage = null;

        try
        {
            var shoppingList = await _shoppingListApiClient.GetCurrentAsync(cancellationToken);
            if (requestVersion == _requestVersion)
            {
                ApplyShoppingList(shoppingList);
            }
        }
        catch (Exception exception)
        {
            if (requestVersion == _requestVersion)
            {
                ErrorMessage = exception.Message;
            }
        }
    }

    public async Task ToggleItemAsync(ShoppingListSummaryItemViewModel? item, CancellationToken cancellationToken = default)
    {
        if (item is null)
        {
            return;
        }

        var requestVersion = Interlocked.Increment(ref _requestVersion);
        ErrorMessage = null;

        try
        {
            var shoppingList = await _shoppingListApiClient.ToggleItemAsync(item.Id, cancellationToken);
            if (requestVersion == _requestVersion)
            {
                ApplyShoppingList(shoppingList);
            }
        }
        catch (Exception exception)
        {
            if (requestVersion == _requestVersion)
            {
                ErrorMessage = exception.Message;
            }
        }
    }

    private void ApplyShoppingList(ShoppingListDto shoppingList)
    {
        var mappedItems = shoppingList.SummaryItems
            .Select(item => new ShoppingListSummaryItemViewModel(item.Id, item.Name, item.Quantity, item.Unit, item.IsChecked))
            .ToArray();

        SummaryItems.Clear();
        foreach (var item in mappedItems)
        {
            SummaryItems.Add(item);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ShoppingListSummaryItemViewModel
{
    public ShoppingListSummaryItemViewModel(Guid id, string name, decimal quantity, string unit, bool isChecked)
    {
        Id = id;
        Name = name;
        Quantity = quantity;
        Unit = unit;
        IsChecked = isChecked;
    }

    public Guid Id { get; }

    public string Name { get; }

    public decimal Quantity { get; }

    public string Unit { get; }

    public bool IsChecked { get; }

    public string QuantityText => $"{Quantity.ToString("0.##", CultureInfo.InvariantCulture)} {Unit}".Trim();

    public string ActionLabel => IsChecked ? "Undo" : "Check";
}
