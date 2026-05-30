using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class LoginViewModelTests
{
    [Fact]
    public async Task LoginCommand_ShouldStoreSessionAndNavigateToHome()
    {
        var authClient = new FakeAuthApiClient(new AuthSession("mobile-token"));
        var sessionStore = new RecordingSessionStore();
        var navigator = new RecordingNavigator();
        var viewModel = new LoginViewModel(authClient, sessionStore, navigator);

        await viewModel.LoginWithGoogleCommand.ExecuteAsync(null);

        Assert.Equal("mobile-token", sessionStore.StoredAccessToken);
        Assert.Equal("//home", navigator.LastRoute);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoginCommand_ShouldNotifyErrorMessageAndAvoidNavigation_WhenLoginFails()
    {
        var authClient = new ThrowingAuthApiClient(new InvalidOperationException("Google sign-in failed."));
        var sessionStore = new RecordingSessionStore();
        var navigator = new RecordingNavigator();
        var viewModel = new LoginViewModel(authClient, sessionStore, navigator);
        var changedProperties = new List<string?>();

        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        await viewModel.LoginWithGoogleCommand.ExecuteAsync(null);

        Assert.Equal("Google sign-in failed.", viewModel.ErrorMessage);
        Assert.Contains(nameof(LoginViewModel.ErrorMessage), changedProperties);
        Assert.Null(sessionStore.StoredAccessToken);
        Assert.Null(navigator.LastRoute);
    }

    [Fact]
    public void LoginPage_ShouldShowBrandedLogoAndWordmark()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "DietPlanner.Mobile",
            "Views",
            "LoginPage.xaml"));

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("miauplanner_logo.svg", xaml);
        Assert.Contains("HeroTitleStyle", xaml);
        Assert.Contains("FormattedString", xaml);
    }

    private sealed class FakeAuthApiClient : IAuthApiClient
    {
        private readonly AuthSession _session;

        public FakeAuthApiClient(AuthSession session)
        {
            _session = session;
        }

        public Task<AuthSession> LoginWithGoogleAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_session);
        }
    }

    private sealed class ThrowingAuthApiClient : IAuthApiClient
    {
        private readonly Exception _exception;

        public ThrowingAuthApiClient(Exception exception)
        {
            _exception = exception;
        }

        public Task<AuthSession> LoginWithGoogleAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<AuthSession>(_exception);
        }
    }

    private sealed class RecordingSessionStore : ISessionStore
    {
        public AuthSession? CurrentSession { get; private set; }

        public string? StoredAccessToken => CurrentSession?.AccessToken;

        public void SetSession(AuthSession session)
        {
            CurrentSession = session;
        }
    }

    private sealed class RecordingNavigator : IAppNavigator
    {
        public string? LastRoute { get; private set; }

        public Task GoToAsync(string route)
        {
            LastRoute = route;
            return Task.CompletedTask;
        }

        public Task GoToMainTabAsync(MainAppTab tab)
        {
            LastRoute = tab == MainAppTab.Home ? "//home" : "//main/shopping-list";
            return Task.CompletedTask;
        }
    }
}
