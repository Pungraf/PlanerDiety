using DietPlanner.Mobile.ViewModels;

namespace DietPlanner.Mobile.Views;

public partial class ShoppingListCreatePage : ContentPage
{
    private readonly ShoppingListCreateViewModel _viewModel;

    public ShoppingListCreatePage(ShoppingListCreateViewModel viewModel)
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
