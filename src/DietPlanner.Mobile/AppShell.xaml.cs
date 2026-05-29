using DietPlanner.Mobile.Views;

namespace DietPlanner.Mobile;

public partial class AppShell : Shell
{
    public AppShell(LoginPage loginPage, HomePage homePage, ShoppingListPage shoppingListPage)
    {
        InitializeComponent();
        LoginShellContent.Content = loginPage;
        HomeShellContent.Content = homePage;
        ShoppingListShellContent.Content = shoppingListPage;
        Routing.RegisterRoute("meal-search", typeof(MealSearchPage));
        Routing.RegisterRoute("meal-details", typeof(MealDetailsPage));
        Routing.RegisterRoute("shopping-list-create", typeof(ShoppingListCreatePage));
    }
}
