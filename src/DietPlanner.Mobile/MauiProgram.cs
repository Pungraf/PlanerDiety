using DietPlanner.Mobile.Navigation;
using DietPlanner.Mobile.Services;
using DietPlanner.Mobile.ViewModels;

namespace DietPlanner.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("https://10.0.2.2:5001/")
        });
        builder.Services.AddSingleton<IGoogleIdTokenProvider, StubGoogleIdTokenProvider>();
        builder.Services.AddSingleton<IAuthApiClient, AuthApiClient>();
        builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
        builder.Services.AddSingleton<IAppNavigator, ShellNavigator>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
