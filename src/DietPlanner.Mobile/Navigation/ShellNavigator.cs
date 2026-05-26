namespace DietPlanner.Mobile.Navigation;

public sealed class ShellNavigator : IAppNavigator
{
    public Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }
}
