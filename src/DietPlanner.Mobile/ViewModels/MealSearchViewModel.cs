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
            _ = LoadMealsAsync(CancellationToken.None);
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
            await _plansApiClient.ReplaceMealAsync(context.Date, context.SlotType, meal.Id);
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
            var meals = await _plansApiClient.SearchMealsAsync(SearchText, cancellationToken);
            var mappedMeals = meals
                .Select(meal => new MealSearchMealViewModel(meal.Id, meal.Name, meal.Type, meal.Kcal, meal.Protein))
                .OrderBy(meal => meal.Name)
                .ToArray();

            Meals.Clear();
            foreach (var meal in mappedMeals)
            {
                Meals.Add(meal);
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
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
