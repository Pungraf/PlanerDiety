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
    private readonly ISelectedPlanContextStore _selectedPlanContextStore;
    private HomePlanViewModel? _currentPlan;
    private HomePlanViewModel? _futurePlan;
    private HomePlanViewModel? _selectedPlan;
    private HomeDayViewModel? _selectedDay;
    private HomeDayViewModel? _copyTargetDay;
    private string? _errorMessage;
    private bool _isLoading;
    private bool _isCopyDayPickerOpen;
    private bool _canGenerateFutureWeek;

    public HomeViewModel(
        IPlansApiClient plansApiClient,
        IAppNavigator navigator,
        IUserPromptService promptService,
        IMealSearchContextStore mealSearchContextStore,
        IMealDetailsContextStore mealDetailsContextStore,
        ISelectedPlanContextStore selectedPlanContextStore)
    {
        _plansApiClient = plansApiClient;
        _navigator = navigator;
        _promptService = promptService;
        _mealSearchContextStore = mealSearchContextStore;
        _mealDetailsContextStore = mealDetailsContextStore;
        _selectedPlanContextStore = selectedPlanContextStore;
        LoadCommand = new AsyncCommand(_ => LoadAsync());
        GenerateFutureWeekCommand = new AsyncCommand(_ => GenerateFutureWeekAsync(), () => CanGenerateFutureWeek);
        SelectCurrentWeekCommand = new AsyncCommand(_ => SelectCurrentWeekAsync(), () => CurrentPlan is not null);
        SelectFutureWeekCommand = new AsyncCommand(_ => SelectFutureWeekAsync(), () => FuturePlan is not null);
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

    public AsyncCommand GenerateFutureWeekCommand { get; }

    public AsyncCommand SelectCurrentWeekCommand { get; }

    public AsyncCommand SelectFutureWeekCommand { get; }

    public AsyncCommand OpenCopyDayCommand { get; }

    public AsyncCommand ConfirmCopyDayCommand { get; }

    public AsyncCommand CancelCopyDayCommand { get; }

    public AsyncCommand OpenMealSearchCommand { get; }

    public AsyncCommand OpenMealDetailsCommand { get; }

    public AsyncCommand GoToHomeCommand { get; }

    public AsyncCommand GoToShoppingListsCommand { get; }

    public HomePlanViewModel? CurrentPlan
    {
        get => _currentPlan;
        private set
        {
            if (_currentPlan == value)
            {
                return;
            }

            _currentPlan = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFutureWeek));
            OnPropertyChanged(nameof(IsCurrentWeekSelected));
            SelectCurrentWeekCommand.RaiseCanExecuteChanged();
        }
    }

    public HomePlanViewModel? FuturePlan
    {
        get => _futurePlan;
        private set
        {
            if (_futurePlan == value)
            {
                return;
            }

            _futurePlan = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFutureWeek));
            OnPropertyChanged(nameof(IsFutureWeekSelected));
            SelectFutureWeekCommand.RaiseCanExecuteChanged();
        }
    }

    public Guid? SelectedPlanId => _selectedPlan?.Id;

    public string SelectedWeekLabel => _selectedPlan?.Label ?? "Current week";

    public bool HasFutureWeek => FuturePlan is not null;

    public bool IsCurrentWeekSelected => SelectedPlanId == CurrentPlan?.Id;

    public bool IsFutureWeekSelected => SelectedPlanId == FuturePlan?.Id;

    public bool CanGenerateFutureWeek
    {
        get => _canGenerateFutureWeek;
        private set
        {
            if (_canGenerateFutureWeek == value)
            {
                return;
            }

            _canGenerateFutureWeek = value;
            OnPropertyChanged();
            GenerateFutureWeekCommand.RaiseCanExecuteChanged();
        }
    }

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
        await LoadAsync(preferredPlanId: _selectedPlanContextStore.SelectedPlanId, preferredDate: null, cancellationToken);
    }

    private async Task LoadAsync(Guid? preferredPlanId, DateOnly? preferredDate, CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var state = await _plansApiClient.GetPlanningStateAsync(cancellationToken);
            ApplyPlanningState(state, preferredPlanId, preferredDate);
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

    private void ApplyPlanningState(PlanningStateDto state, Guid? preferredPlanId, DateOnly? preferredDate)
    {
        CurrentPlan = MapPlan("Current week", state.CurrentPlan);
        FuturePlan = state.FuturePlan is null ? null : MapPlan("Next week", state.FuturePlan);
        CanGenerateFutureWeek = state.CanGenerateFutureWeek;

        var nextSelectedPlan = ResolveSelectedPlan(preferredPlanId)
            ?? ResolveSelectedPlan(_selectedPlanContextStore.SelectedPlanId)
            ?? CurrentPlan;

        ApplySelectedPlan(nextSelectedPlan, preferredDate);
    }

    private HomePlanViewModel? ResolveSelectedPlan(Guid? preferredPlanId)
    {
        if (!preferredPlanId.HasValue)
        {
            return null;
        }

        if (CurrentPlan?.Id == preferredPlanId.Value)
        {
            return CurrentPlan;
        }

        if (FuturePlan?.Id == preferredPlanId.Value)
        {
            return FuturePlan;
        }

        return null;
    }

    private void ApplySelectedPlan(HomePlanViewModel? selectedPlan, DateOnly? preferredDate)
    {
        _selectedPlan = selectedPlan;
        _selectedPlanContextStore.SelectedPlanId = selectedPlan?.Id;
        OnPropertyChanged(nameof(SelectedPlanId));
        OnPropertyChanged(nameof(SelectedWeekLabel));
        OnPropertyChanged(nameof(IsCurrentWeekSelected));
        OnPropertyChanged(nameof(IsFutureWeekSelected));

        var mappedDays = selectedPlan?.Days.OrderBy(day => day.Date).ToArray() ?? [];
        var today = DateOnly.FromDateTime(DateTime.Today);
        var defaultDate = mappedDays.Any(day => day.Date == today)
            ? today
            : mappedDays.FirstOrDefault()?.Date;
        var selectedDate = preferredDate ?? SelectedDay?.Date ?? defaultDate;

        Days.Clear();
        foreach (var day in mappedDays)
        {
            Days.Add(day);
        }

        SelectedDay = mappedDays.FirstOrDefault(day => day.Date == selectedDate)
            ?? mappedDays.FirstOrDefault();
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
            if (SelectedPlanId.HasValue)
            {
                await _plansApiClient.CopyDayAsync(SelectedPlanId.Value, SelectedDay.Date, targetDay.Date, deleteLinkedShoppingLists: false);
            }
            else
            {
                await _plansApiClient.CopyDayAsync(SelectedDay.Date, targetDay.Date, deleteLinkedShoppingLists: false);
            }

            IsCopyDayPickerOpen = false;
            CopyTargetDay = null;
            await LoadAsync(SelectedPlanId, targetDay.Date, CancellationToken.None);
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

            if (SelectedPlanId.HasValue)
            {
                await _plansApiClient.CopyDayAsync(SelectedPlanId.Value, SelectedDay.Date, targetDay.Date, deleteLinkedShoppingLists: true);
            }
            else
            {
                await _plansApiClient.CopyDayAsync(SelectedDay.Date, targetDay.Date, deleteLinkedShoppingLists: true);
            }

            IsCopyDayPickerOpen = false;
            CopyTargetDay = null;
            await LoadAsync(SelectedPlanId, targetDay.Date, CancellationToken.None);
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

        _mealSearchContextStore.Current = new MealSearchContext(SelectedPlanId, SelectedDay.Date, slot.SlotType, slot.Name);
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

    private static HomePlanViewModel MapPlan(string label, CurrentPlanDto plan)
    {
        var startDate = ParseApiDate(plan.StartDate);
        var days = plan.Days
            .Select(MapDay)
            .OrderBy(day => day.Date)
            .ToArray();

        return new HomePlanViewModel(plan.Id, label, startDate, days);
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

    private Task SelectCurrentWeekAsync()
    {
        if (CurrentPlan is not null)
        {
            ApplySelectedPlan(CurrentPlan, preferredDate: null);
        }

        return Task.CompletedTask;
    }

    private Task SelectFutureWeekAsync()
    {
        if (FuturePlan is not null)
        {
            ApplySelectedPlan(FuturePlan, preferredDate: null);
        }

        return Task.CompletedTask;
    }

    private async Task GenerateFutureWeekAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var state = await _plansApiClient.GenerateFutureWeekAsync();
            ApplyPlanningState(state, CurrentPlan?.Id, SelectedDay?.Date);
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
}

public sealed class HomePlanViewModel
{
    public HomePlanViewModel(Guid id, string label, DateOnly startDate, IReadOnlyList<HomeDayViewModel> days)
    {
        Id = id;
        Label = label;
        StartDate = startDate;
        Days = days;
    }

    public Guid Id { get; }

    public string Label { get; }

    public DateOnly StartDate { get; }

    public IReadOnlyList<HomeDayViewModel> Days { get; }
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
