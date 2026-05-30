using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class MealSearchViewModel : INotifyPropertyChanged
{
    private readonly IPlansApiClient _plansApiClient;
    private readonly IMealSearchContextStore _mealSearchContextStore;
    private readonly IAppNavigator _navigator;
    private readonly IUserPromptService _promptService;
    private IReadOnlyList<MealSearchMealViewModel> _catalog = [];
    private string? _searchText;
    private string? _errorMessage;

    public MealSearchViewModel(
        IPlansApiClient plansApiClient,
        IMealSearchContextStore mealSearchContextStore,
        IAppNavigator navigator,
        IUserPromptService promptService)
    {
        _plansApiClient = plansApiClient;
        _mealSearchContextStore = mealSearchContextStore;
        _navigator = navigator;
        _promptService = promptService;
        LoadCommand = new AsyncCommand(_ => LoadAsync());
        ReplaceMealCommand = new AsyncCommand(meal => ReplaceMealAsync(meal as MealSearchMealViewModel));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MealSearchMealViewModel> Meals { get; } = [];

    public AsyncCommand LoadCommand { get; }

    public AsyncCommand ReplaceMealCommand { get; }

    public string ScreenTitle => _mealSearchContextStore.Current is null
        ? "Replace meal"
        : $"Replace {_mealSearchContextStore.Current.SlotType}";

    public string CurrentMealName => _mealSearchContextStore.Current?.CurrentMealName ?? "No meal selected";

    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            ApplyFilter();
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

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(ScreenTitle));
        OnPropertyChanged(nameof(CurrentMealName));

        try
        {
            await LoadMealsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private async Task ReplaceMealAsync(MealSearchMealViewModel? meal)
    {
        var context = _mealSearchContextStore.Current;
        if (context is null || meal is null)
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            if (context.PlanId.HasValue)
            {
                await _plansApiClient.ReplaceMealAsync(context.PlanId.Value, context.Date, context.SlotType, meal.Id, deleteLinkedShoppingLists: false);
            }
            else
            {
                await _plansApiClient.ReplaceMealAsync(context.Date, context.SlotType, meal.Id, deleteLinkedShoppingLists: false);
            }

            await _navigator.GoToAsync("//home");
            _mealSearchContextStore.Current = null;
        }
        catch (LinkedShoppingListsExistException)
        {
            var confirmed = await _promptService.ConfirmAsync(
                "Delete shopping lists?",
                "This week has linked shopping lists. Changing the plan will delete them.",
                "Continue",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            if (context.PlanId.HasValue)
            {
                await _plansApiClient.ReplaceMealAsync(context.PlanId.Value, context.Date, context.SlotType, meal.Id, deleteLinkedShoppingLists: true);
            }
            else
            {
                await _plansApiClient.ReplaceMealAsync(context.Date, context.SlotType, meal.Id, deleteLinkedShoppingLists: true);
            }

            await _navigator.GoToAsync("//home");
            _mealSearchContextStore.Current = null;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private async Task LoadMealsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_catalog.Count == 0)
            {
                var meals = await _plansApiClient.GetMealCatalogAsync(cancellationToken);
                _catalog = meals
                    .Select(meal => new MealSearchMealViewModel(meal.Id, meal.Name, meal.Type, meal.Kcal, meal.Protein))
                    .OrderBy(meal => meal.Name)
                    .ToArray();
            }

            ApplyFilter();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void ApplyFilter()
    {
        var filteredMeals = string.IsNullOrWhiteSpace(SearchText)
            ? _catalog
            : _catalog.Where(meal => meal.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();

        Meals.Clear();
        foreach (var meal in filteredMeals)
        {
            Meals.Add(meal);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class MealSearchMealViewModel
{
    public MealSearchMealViewModel(Guid id, string name, string type, int kcal, int protein)
    {
        Id = id;
        Name = name;
        Type = type;
        Kcal = kcal;
        Protein = protein;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Type { get; }

    public int Kcal { get; }

    public int Protein { get; }

    public string MacroSummary => $"{Kcal} kcal • {Protein} g protein";
}
