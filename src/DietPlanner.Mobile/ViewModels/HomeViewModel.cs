using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class HomeViewModel : INotifyPropertyChanged
{
    private const string ApiDateFormat = "yyyy-MM-dd";
    private readonly IPlansApiClient _plansApiClient;
    private readonly IAppNavigator _navigator;
    private readonly IUserPromptService _promptService;
    private readonly IMealSearchContextStore _mealSearchContextStore;
    private readonly IMealDetailsContextStore _mealDetailsContextStore;
    private HomeDayViewModel? _selectedDay;
    private HomeDayViewModel? _copyTargetDay;
    private string? _errorMessage;
    private bool _isLoading;
    private bool _isCopyDayPickerOpen;

    public HomeViewModel(
        IPlansApiClient plansApiClient,
        IAppNavigator navigator,
        IUserPromptService promptService,
        IMealSearchContextStore mealSearchContextStore,
        IMealDetailsContextStore mealDetailsContextStore)
    {
        _plansApiClient = plansApiClient;
        _navigator = navigator;
        _promptService = promptService;
        _mealSearchContextStore = mealSearchContextStore;
        _mealDetailsContextStore = mealDetailsContextStore;
        LoadCommand = new AsyncCommand(_ => LoadAsync());
        OpenCopyDayCommand = new AsyncCommand(_ => OpenCopyDayAsync(), () => SelectedDay is not null);
        ConfirmCopyDayCommand = new AsyncCommand(_ => ConfirmCopyDayAsync(), () => SelectedDay is not null && CopyTargetDay is not null);
        CancelCopyDayCommand = new AsyncCommand(_ => CancelCopyDayAsync());
        OpenMealSearchCommand = new AsyncCommand(slot => OpenMealSearchAsync(slot as HomeMealSlotViewModel), () => SelectedDay is not null);
        OpenMealDetailsCommand = new AsyncCommand(slot => OpenMealDetailsAsync(slot as HomeMealSlotViewModel), () => SelectedDay is not null);
        GoToHomeCommand = new AsyncCommand(_ => _navigator.GoToMainTabAsync(MainAppTab.Home));
        GoToShoppingListsCommand = new AsyncCommand(_ => _navigator.GoToMainTabAsync(MainAppTab.ShoppingList));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<HomeDayViewModel> Days { get; } = [];

    public AsyncCommand LoadCommand { get; }

    public AsyncCommand OpenCopyDayCommand { get; }

    public AsyncCommand ConfirmCopyDayCommand { get; }

    public AsyncCommand CancelCopyDayCommand { get; }

    public AsyncCommand OpenMealSearchCommand { get; }

    public AsyncCommand OpenMealDetailsCommand { get; }

    public AsyncCommand GoToHomeCommand { get; }

    public AsyncCommand GoToShoppingListsCommand { get; }

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
            OnPropertyChanged(nameof(HasSelectedDay));
            OnPropertyChanged(nameof(AvailableCopyTargetDays));
            if (_copyTargetDay?.Date == _selectedDay?.Date)
            {
                CopyTargetDay = AvailableCopyTargetDays.FirstOrDefault();
            }

            OpenCopyDayCommand.RaiseCanExecuteChanged();
            ConfirmCopyDayCommand.RaiseCanExecuteChanged();
            OpenMealSearchCommand.RaiseCanExecuteChanged();
            OpenMealDetailsCommand.RaiseCanExecuteChanged();
        }
    }

    public HomeDayViewModel? CopyTargetDay
    {
        get => _copyTargetDay;
        set
        {
            if (_copyTargetDay == value)
            {
                return;
            }

            _copyTargetDay = value;
            OnPropertyChanged();
            ConfirmCopyDayCommand.RaiseCanExecuteChanged();
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
        }
    }

    public bool HasSelectedDay => SelectedDay is not null;

    public bool IsCopyDayPickerOpen
    {
        get => _isCopyDayPickerOpen;
        private set
        {
            if (_isCopyDayPickerOpen == value)
            {
                return;
            }

            _isCopyDayPickerOpen = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<HomeDayViewModel> AvailableCopyTargetDays
        => Days.Where(day => day.Date != SelectedDay?.Date).ToArray();

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
        IsLoading = true;

        try
        {
            var plan = await _plansApiClient.GetCurrentPlanAsync(cancellationToken);
            var startDate = ParseApiDate(plan.StartDate);
            var mappedDays = plan.Days
                .Select(MapDay)
                .OrderBy(day => day.Date)
                .ToArray();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var defaultDate = mappedDays.Any(day => day.Date == today) ? today : startDate;
            var selectedDate = preferredDate ?? SelectedDay?.Date ?? defaultDate;

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
        finally
        {
            IsLoading = false;
        }
    }

    private Task OpenCopyDayAsync()
    {
        if (SelectedDay is null)
        {
            return Task.CompletedTask;
        }

        ErrorMessage = null;
        CopyTargetDay = AvailableCopyTargetDays.FirstOrDefault();
        IsCopyDayPickerOpen = true;
        return Task.CompletedTask;
    }

    private Task CancelCopyDayAsync()
    {
        IsCopyDayPickerOpen = false;
        CopyTargetDay = null;
        return Task.CompletedTask;
    }

    private async Task ConfirmCopyDayAsync()
    {
        if (SelectedDay is null || CopyTargetDay is null || CopyTargetDay.Date == SelectedDay.Date)
        {
            return;
        }

        var targetDay = CopyTargetDay;

        ErrorMessage = null;

        try
        {
            await _plansApiClient.CopyDayAsync(SelectedDay.Date, targetDay.Date, deleteLinkedShoppingLists: false);
            IsCopyDayPickerOpen = false;
            CopyTargetDay = null;
            await LoadAsync(targetDay.Date, CancellationToken.None);
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

            await _plansApiClient.CopyDayAsync(SelectedDay.Date, targetDay.Date, deleteLinkedShoppingLists: true);
            IsCopyDayPickerOpen = false;
            CopyTargetDay = null;
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

        _mealSearchContextStore.Current = new MealSearchContext(null, SelectedDay.Date, slot.SlotType, slot.Name);
        await _navigator.GoToAsync("meal-search");
    }

    private async Task OpenMealDetailsAsync(HomeMealSlotViewModel? slot)
    {
        ErrorMessage = null;

        if (slot?.MealId is null)
        {
            ErrorMessage = "Recipe details are not available for this meal.";
            return;
        }

        try
        {
            _mealDetailsContextStore.Current = new MealDetailsContext(slot.MealId.Value);
            await _navigator.GoToAsync("meal-details");
        }
        catch (Exception)
        {
            _mealDetailsContextStore.Current = null;
            ErrorMessage = "Could not open recipe details.";
        }
    }

    private static HomeDayViewModel MapDay(PlanDayDto day)
    {
        var parsedDate = ParseApiDate(day.Date);
        var meals = day.Meals.Select(slot => new HomeMealSlotViewModel(
            slot.SlotType,
            slot.MealId,
            slot.Name,
            slot.Kcal,
            slot.Protein)).ToArray();

        return new HomeDayViewModel(parsedDate, meals);
    }

    private static DateOnly ParseApiDate(string value)
    {
        if (DateOnly.TryParseExact(value, ApiDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }

        throw new InvalidOperationException("The API returned an invalid plan date.");
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
    public HomeMealSlotViewModel(string slotType, Guid? mealId, string name, int kcal, int protein)
    {
        SlotType = slotType;
        MealId = mealId;
        Name = string.IsNullOrWhiteSpace(name) ? "Choose meal" : name;
        Kcal = kcal;
        Protein = protein;
    }

    public string SlotType { get; }

    public Guid? MealId { get; }

    public string SlotLabel => char.ToUpperInvariant(SlotType[0]) + SlotType[1..];

    public string Name { get; }

    public int Kcal { get; }

    public int Protein { get; }

    public string MacroSummary => $"{Kcal} kcal / {Protein} g protein";
}
