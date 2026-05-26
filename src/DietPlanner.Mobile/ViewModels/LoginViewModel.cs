using DietPlanner.Mobile.Commands;
using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DietPlanner.Mobile.ViewModels;

public sealed class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthApiClient _authApiClient;
    private readonly ISessionStore _sessionStore;
    private readonly IAppNavigator _navigator;
    private string? _errorMessage;

    public LoginViewModel(IAuthApiClient authApiClient, ISessionStore sessionStore, IAppNavigator navigator)
    {
        _authApiClient = authApiClient;
        _sessionStore = sessionStore;
        _navigator = navigator;
        LoginWithGoogleCommand = new AsyncCommand(_ => LoginAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AsyncCommand LoginWithGoogleCommand { get; }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
