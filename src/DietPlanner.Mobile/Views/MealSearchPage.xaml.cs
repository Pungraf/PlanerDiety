using DietPlanner.Mobile.ViewModels;

namespace DietPlanner.Mobile.Views;

public partial class MealSearchPage : ContentPage
{
    private readonly MealSearchViewModel _viewModel;

    public MealSearchPage(MealSearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
