namespace DietPlanner.Mobile.Navigation;

public interface IAppNavigator
{
    Task GoToAsync(string route);

    Task GoToMainTabAsync(MainAppTab tab);
}
