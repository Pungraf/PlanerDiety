using DietPlanner.Mobile.ViewModels;

namespace DietPlanner.Mobile.Views;

public partial class MealDetailsPage : ContentPage
{
    private readonly MealDetailsViewModel _viewModel;

    public MealDetailsPage(MealDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.LoadAsync();
        }
        catch
        {
            // Final safety net: expected failures should already be absorbed in the viewmodel.
        }
    }
}
