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
    private List<MealSearchMealViewModel> _allMeals = [];
    private string? _searchText;
    private string? _errorMessage;

    public MealSearchViewModel(IPlansApiClient plansApiClient, IMealSearchContextStore mealSearchContextStore, IAppNavigator navigator)
    {
        _plansApiClient = plansApiClient;
        _mealSearchContextStore = mealSearchContextStore;
        _navigator = navigator;
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
            var meals = await _plansApiClient.SearchMealsAsync(null, cancellationToken);
            _allMeals = meals
                .Select(meal => new MealSearchMealViewModel(meal.Id, meal.Name, meal.Type, meal.Kcal, meal.Protein))
                .OrderBy(meal => meal.Name)
                .ToList();

            ApplyFilter();
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
            await _plansApiClient.ReplaceMealAsync(context.Date, context.SlotType, meal.Id);
            _mealSearchContextStore.Current = null;
            await _navigator.GoToAsync("//home");
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void ApplyFilter()
    {
        var filteredMeals = string.IsNullOrWhiteSpace(SearchText)
            ? _allMeals
            : _allMeals
                .Where(meal => meal.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

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
