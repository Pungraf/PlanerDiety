using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class MealDetailsViewModel : INotifyPropertyChanged
{
    private readonly IPlansApiClient _plansApiClient;
    private readonly IMealDetailsContextStore _contextStore;
    private string _name = string.Empty;
    private int _kcal;
    private int _protein;
    private string _description = string.Empty;
    private string? _errorMessage;
    private bool _isLoading;

    public MealDetailsViewModel(IPlansApiClient plansApiClient, IMealDetailsContextStore contextStore)
    {
        _plansApiClient = plansApiClient;
        _contextStore = contextStore;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MealDetailsIngredientViewModel> Ingredients { get; } = [];

    public string Name
    {
        get => _name;
        private set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    public int Kcal
    {
        get => _kcal;
        private set
        {
            if (_kcal == value) return;
            _kcal = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MacroSummary));
        }
    }

    public int Protein
    {
        get => _protein;
        private set
        {
            if (_protein == value) return;
            _protein = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MacroSummary));
        }
    }

    public string Description
    {
        get => _description;
        private set
        {
            if (_description == value) return;
            _description = value;
            OnPropertyChanged();
        }
    }

    public string MacroSummary => $"{Kcal} kcal • {Protein} g protein";

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasContent));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(HasContent));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasContent => !IsLoading && !HasError;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var mealId = _contextStore.Current?.MealId;
            if (mealId is null)
            {
                ErrorMessage = "Could not load recipe details.";
                return;
            }

            var details = await _plansApiClient.GetMealDetailsAsync(mealId.Value, cancellationToken);
            Name = details.Name;
            Kcal = details.Kcal;
            Protein = details.Protein;
            Description = details.Description;

            Ingredients.Clear();
            foreach (var ingredient in details.Ingredients.Select(i => new MealDetailsIngredientViewModel(i.Name, i.Quantity, i.Unit, i.Category)))
            {
                Ingredients.Add(ingredient);
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Could not load recipe details.";
            Ingredients.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class MealDetailsIngredientViewModel
{
    public MealDetailsIngredientViewModel(string name, decimal quantity, string unit, string category)
    {
        Name = name;
        Quantity = quantity;
        Unit = unit;
        Category = category;
    }

    public string Name { get; }
    public decimal Quantity { get; }
    public string Unit { get; }
    public string Category { get; }
    public string QuantityText => $"{Quantity:0.##} {Unit}".Trim();
}
