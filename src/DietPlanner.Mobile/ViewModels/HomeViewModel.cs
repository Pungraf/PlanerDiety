using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class HomeViewModel : INotifyPropertyChanged
{
    private readonly IPlansApiClient _plansApiClient;
    private readonly IAppNavigator _navigator;
    private readonly IMealSearchContextStore _mealSearchContextStore;
    private HomeDayViewModel? _selectedDay;
    private string? _errorMessage;

    public HomeViewModel(IPlansApiClient plansApiClient, IAppNavigator navigator, IMealSearchContextStore mealSearchContextStore)
    {
        _plansApiClient = plansApiClient;
        _navigator = navigator;
        _mealSearchContextStore = mealSearchContextStore;
        LoadCommand = new AsyncCommand(_ => LoadAsync());
        CopyDayCommand = new AsyncCommand(target => CopyDayAsync(target as HomeDayViewModel), () => SelectedDay is not null);
        OpenMealSearchCommand = new AsyncCommand(slot => OpenMealSearchAsync(slot as HomeMealSlotViewModel), () => SelectedDay is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<HomeDayViewModel> Days { get; } = [];

    public AsyncCommand LoadCommand { get; }

    public AsyncCommand CopyDayCommand { get; }

    public AsyncCommand OpenMealSearchCommand { get; }

    public HomeDayViewModel? SelectedDay
    {
        get => _selectedDay;
        set
        {
            if (_selectedDay == value)
            {
                return;
            }

            _selectedDay = value;
            OnPropertyChanged();
            CopyDayCommand.RaiseCanExecuteChanged();
            OpenMealSearchCommand.RaiseCanExecuteChanged();
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
        await LoadAsync(preferredDate: null, cancellationToken);
    }

    private async Task LoadAsync(DateOnly? preferredDate, CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        try
        {
            var plan = await _plansApiClient.GetCurrentPlanAsync(cancellationToken);
            DateOnly? startDate = DateOnly.TryParse(plan.StartDate, out var parsedStartDate)
                ? parsedStartDate
                : null;
            var selectedDate = preferredDate ?? SelectedDay?.Date ?? startDate;

            var mappedDays = plan.Days
                .Select(MapDay)
                .OrderBy(day => day.Date)
                .ToArray();

            Days.Clear();
            foreach (var day in mappedDays)
            {
                Days.Add(day);
            }

            SelectedDay = mappedDays.FirstOrDefault(day => day.Date == selectedDate)
                ?? mappedDays.FirstOrDefault();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private async Task CopyDayAsync(HomeDayViewModel? targetDay)
    {
        if (SelectedDay is null || targetDay is null || targetDay.Date == SelectedDay.Date)
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            await _plansApiClient.CopyDayAsync(SelectedDay.Date, targetDay.Date);
            await LoadAsync(targetDay.Date, CancellationToken.None);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private async Task OpenMealSearchAsync(HomeMealSlotViewModel? slot)
    {
        if (SelectedDay is null || slot is null)
        {
            return;
        }

        _mealSearchContextStore.Current = new MealSearchContext(SelectedDay.Date, slot.SlotType, slot.Name);
        await _navigator.GoToAsync("meal-search");
    }

    private static HomeDayViewModel MapDay(PlanDayDto day)
    {
        var parsedDate = DateOnly.Parse(day.Date);
        var meals = day.Meals.Select(slot => new HomeMealSlotViewModel(
            slot.SlotType,
            slot.Name,
            slot.Kcal,
            slot.Protein)).ToArray();

        return new HomeDayViewModel(parsedDate, meals);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class HomeDayViewModel
{
    public HomeDayViewModel(DateOnly date, IReadOnlyList<HomeMealSlotViewModel> meals)
    {
        Date = date;
        Meals = meals;
        TotalKcal = meals.Sum(meal => meal.Kcal);
        TotalProtein = meals.Sum(meal => meal.Protein);
    }

    public DateOnly Date { get; }

    public string DisplayDate => Date.ToString("ddd, dd MMM");

    public IReadOnlyList<HomeMealSlotViewModel> Meals { get; }

    public int TotalKcal { get; }

    public int TotalProtein { get; }
}

public sealed class HomeMealSlotViewModel
{
    public HomeMealSlotViewModel(string slotType, string name, int kcal, int protein)
    {
        SlotType = slotType;
        Name = string.IsNullOrWhiteSpace(name) ? "Choose meal" : name;
        Kcal = kcal;
        Protein = protein;
    }

    public string SlotType { get; }

    public string SlotLabel => char.ToUpperInvariant(SlotType[0]) + SlotType[1..];

    public string Name { get; }

    public int Kcal { get; }

    public int Protein { get; }
}
