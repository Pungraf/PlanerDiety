using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class ShoppingListCreateViewModel : INotifyPropertyChanged
{
    private readonly IShoppingListApiClient _shoppingListApiClient;
    private readonly IAppNavigator _navigator;
    private bool _hasCreatedCurrentSelection;
    private string? _errorMessage;
    private ShoppingListCreatePreset? _selectedPreset;
    private bool _isPresetStep = true;

    public ShoppingListCreateViewModel(IShoppingListApiClient shoppingListApiClient, IAppNavigator navigator)
    {
        _shoppingListApiClient = shoppingListApiClient;
        _navigator = navigator;
        LoadCommand = new AsyncCommand(_ => LoadAsync());
        ToggleMealCommand = new AsyncCommand(meal =>
        {
            ToggleMeal(meal as ShoppingListCreateMealViewModel);
            return Task.CompletedTask;
        });
        SelectPresetCommand = new AsyncCommand(preset =>
        {
            if (preset is ShoppingListCreatePreset createPreset)
            {
                SelectPreset(createPreset);
            }

            return Task.CompletedTask;
        });
        SaveCommand = new AsyncCommand(_ => SaveAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ShoppingListCreateDayViewModel> Days { get; } = [];

    public AsyncCommand LoadCommand { get; }

    public AsyncCommand ToggleMealCommand { get; }

    public AsyncCommand SelectPresetCommand { get; }

    public AsyncCommand SaveCommand { get; }

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

    public ShoppingListCreatePreset? SelectedPreset
    {
        get => _selectedPreset;
        private set
        {
            if (_selectedPreset == value)
            {
                return;
            }

            _selectedPreset = value;
            OnPropertyChanged();
        }
    }

    public bool IsPresetStep
    {
        get => _isPresetStep;
        private set
        {
            if (_isPresetStep == value)
            {
                return;
            }

            _isPresetStep = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBuilderStep));
        }
    }

    public bool IsBuilderStep => !IsPresetStep;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _hasCreatedCurrentSelection = false;
        ErrorMessage = null;
        var options = await _shoppingListApiClient.GetCreateOptionsAsync(cancellationToken);
        Days.Clear();
        foreach (var day in options.Select(MapDay))
        {
            Days.Add(day);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = null;

        if (_hasCreatedCurrentSelection)
        {
            ErrorMessage = "Shopping list created, but navigation back to the shopping list failed.";
            return;
        }

        var selectedIngredients = Days
            .SelectMany(day => day.Meals)
            .SelectMany(meal => meal.Ingredients
                .Where(ingredient => ingredient.IsSelected)
                .Select(ingredient => new CreateShoppingListIngredientRequest(meal.Date, meal.SlotType, ingredient.IngredientId)))
            .ToArray();

        try
        {
            await _shoppingListApiClient.CreateAsync("Shopping list", selectedIngredients, cancellationToken);
            _hasCreatedCurrentSelection = true;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            return;
        }

        try
        {
            await _navigator.GoToAsync("//main/shopping-list");
        }
        catch (Exception)
        {
            ErrorMessage = "Shopping list created, but navigation back to the shopping list failed.";
        }
    }

    private static ShoppingListCreateDayViewModel MapDay(ShoppingListCreateDayOptionDto day)
        => new(
            day.Date,
            day.Meals.Select(meal => new ShoppingListCreateMealViewModel(
                day.Date,
                meal.SlotType,
                meal.MealName,
                meal.Ingredients.Select(ingredient => new ShoppingListCreateIngredientViewModel(
                    ingredient.IngredientId,
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.Category,
                    ingredient.IsSelected)).ToArray())).ToArray());

    private void SelectPreset(ShoppingListCreatePreset preset)
    {
        _hasCreatedCurrentSelection = false;
        SelectedPreset = preset;
        switch (preset)
        {
            case ShoppingListCreatePreset.FullWeek:
                SetIngredientSelection(isSelected: true);
                IsPresetStep = false;
                break;
            case ShoppingListCreatePreset.SelectedDays:
                IsPresetStep = false;
                break;
            case ShoppingListCreatePreset.Custom:
                IsPresetStep = false;
                break;
        }
    }

    private void ToggleMeal(ShoppingListCreateMealViewModel? meal)
    {
        if (meal is null)
        {
            return;
        }

        _hasCreatedCurrentSelection = false;
        var nextValue = meal.Ingredients.Any(ingredient => ingredient.IsSelected);
        foreach (var ingredient in meal.Ingredients)
        {
            ingredient.IsSelected = !nextValue;
        }
    }

    private void SetIngredientSelection(bool isSelected)
    {
        _hasCreatedCurrentSelection = false;
        foreach (var ingredient in Days.SelectMany(day => day.Meals).SelectMany(meal => meal.Ingredients))
        {
            ingredient.IsSelected = isSelected;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public enum ShoppingListCreatePreset
{
    FullWeek,
    SelectedDays,
    Custom
}

public sealed class ShoppingListCreateDayViewModel
{
    public ShoppingListCreateDayViewModel(string date, IReadOnlyList<ShoppingListCreateMealViewModel> meals)
    {
        Date = date;
        Meals = meals;
    }

    public string Date { get; }

    public IReadOnlyList<ShoppingListCreateMealViewModel> Meals { get; }
}

public sealed class ShoppingListCreateMealViewModel
{
    public ShoppingListCreateMealViewModel(string date, string slotType, string mealName, IReadOnlyList<ShoppingListCreateIngredientViewModel> ingredients)
    {
        Date = date;
        SlotType = slotType;
        MealName = mealName;
        Ingredients = ingredients;
    }

    public string Date { get; }

    public string SlotType { get; }

    public string MealName { get; }

    public IReadOnlyList<ShoppingListCreateIngredientViewModel> Ingredients { get; }
}

public sealed class ShoppingListCreateIngredientViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public ShoppingListCreateIngredientViewModel(Guid ingredientId, string name, decimal quantity, string unit, string category, bool isSelected)
    {
        IngredientId = ingredientId;
        Name = name;
        Quantity = quantity;
        Unit = unit;
        Category = category;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid IngredientId { get; }

    public string Name { get; }

    public decimal Quantity { get; }

    public string Unit { get; }

    public string Category { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}
