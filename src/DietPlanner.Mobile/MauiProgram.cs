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
        using var settingsStream = FileSystem.OpenAppPackageFileAsync("appsettings.mobile.json").GetAwaiter().GetResult();
        var mobileAppSettings = new MobileAppSettingsLoader().Load(settingsStream);
        var apiBaseAddressResolver = new ApiBaseAddressResolver();
        var apiBaseAddress = apiBaseAddressResolver.Resolve(
            Environment.GetEnvironmentVariable("DIETPLANNER_API_BASE_URL") ?? mobileAppSettings.ApiBaseUrl.ToString());
        var googleSignInSettings = new GoogleSignInSettingsResolver().TryResolve(
            Environment.GetEnvironmentVariable("DIETPLANNER_GOOGLE_WEB_CLIENT_ID") ?? mobileAppSettings.GoogleWebClientId);

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = apiBaseAddress
        });
#if ANDROID
        if (googleSignInSettings is not null)
        {
            builder.Services.AddSingleton(googleSignInSettings);
            builder.Services.AddSingleton<IGoogleIdTokenProvider, AndroidGoogleIdTokenProvider>();
        }
        else
        {
#if DEBUG
            builder.Services.AddSingleton<IGoogleIdTokenProvider, StubGoogleIdTokenProvider>();
#else
            builder.Services.AddSingleton<IGoogleIdTokenProvider, UnsupportedGoogleIdTokenProvider>();
#endif
        }
#elif DEBUG
        builder.Services.AddSingleton<IGoogleIdTokenProvider, StubGoogleIdTokenProvider>();
#else
        builder.Services.AddSingleton<IGoogleIdTokenProvider, UnsupportedGoogleIdTokenProvider>();
#endif
        builder.Services.AddSingleton<IAuthApiClient, AuthApiClient>();
        builder.Services.AddSingleton<IPlansApiClient, PlansApiClient>();
        builder.Services.AddSingleton<IShoppingListApiClient, ShoppingListApiClient>();
        builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
        builder.Services.AddSingleton<IMealSearchContextStore, InMemoryMealSearchContextStore>();
        builder.Services.AddSingleton<IMealDetailsContextStore, InMemoryMealDetailsContextStore>();
        builder.Services.AddSingleton<IUserPromptService, ShellUserPromptService>();
        builder.Services.AddSingleton<IAppNavigator, ShellNavigator>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<MealSearchViewModel>();
        builder.Services.AddTransient<MealDetailsViewModel>();
        builder.Services.AddTransient<ShoppingListViewModel>();
        builder.Services.AddTransient<ShoppingListCreateViewModel>();
        builder.Services.AddTransient<Views.LoginPage>();
        builder.Services.AddTransient<Views.HomePage>();
        builder.Services.AddTransient<Views.MealSearchPage>();
        builder.Services.AddTransient<Views.MealDetailsPage>();
        builder.Services.AddTransient<Views.ShoppingListPage>();
        builder.Services.AddTransient<Views.ShoppingListCreatePage>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
