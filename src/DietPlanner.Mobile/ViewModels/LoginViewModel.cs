using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;

namespace DietPlanner.Mobile.ViewModels;

public sealed class LoginViewModel
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ISessionStore _sessionStore;
    private readonly IAppNavigator _navigator;

    public LoginViewModel(IAuthApiClient authApiClient, ISessionStore sessionStore, IAppNavigator navigator)
    {
        _authApiClient = authApiClient;
        _sessionStore = sessionStore;
        _navigator = navigator;
        LoginWithGoogleCommand = new AsyncCommand(_ => LoginAsync());
    }

    public AsyncCommand LoginWithGoogleCommand { get; }

    public string? ErrorMessage { get; private set; }

    private async Task LoginAsync()
    {
        ErrorMessage = null;

        try
        {
            var session = await _authApiClient.LoginWithGoogleAsync();
            _sessionStore.SetSession(session);
            await _navigator.GoToAsync("//home");
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
    }
}
