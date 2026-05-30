namespace DietPlanner.Mobile.Navigation;

public sealed class ShellNavigator : IAppNavigator
{
    public Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }

    public Task GoToMainTabAsync(MainAppTab tab)
    {
        return tab switch
        {
            MainAppTab.Home => Shell.Current.GoToAsync("//home"),
            MainAppTab.ShoppingList => Shell.Current.GoToAsync("//main/shopping-list"),
            _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null)
        };
    }
}
