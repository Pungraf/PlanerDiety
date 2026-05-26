using DietPlanner.Mobile.Views;

namespace DietPlanner.Mobile;

public partial class AppShell : Shell
{
    public AppShell(LoginPage loginPage, HomePage homePage)
    {
        InitializeComponent();
        LoginShellContent.Content = loginPage;
        HomeShellContent.Content = homePage;
        Routing.RegisterRoute("meal-search", typeof(MealSearchPage));
    }
}
