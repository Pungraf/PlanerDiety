using DietPlanner.Mobile.ViewModels;

namespace DietPlanner.Mobile.Views;

public partial class ShoppingListCreatePage : ContentPage
{
    private readonly ShoppingListCreateViewModel _viewModel;
    private bool _hasLoaded;

    public ShoppingListCreatePage(ShoppingListCreateViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await _viewModel.LoadAsync();
    }
}
