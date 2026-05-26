using DietPlanner.Mobile.ViewModels;

namespace DietPlanner.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = IPlatformApplication.Current?.Services.GetService<LoginViewModel>();
    }
}
