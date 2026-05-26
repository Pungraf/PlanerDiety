using System.Reflection;
using Xunit;

namespace DietPlanner.Mobile.Tests;

public sealed class LoginViewModelTests
{
    [Fact]
    public async Task LoginCommand_ShouldStoreSessionAndNavigateToHome()
    {
        var assembly = Assembly.Load("DietPlanner.Mobile");
        var loginViewModelType = assembly.GetType("DietPlanner.Mobile.ViewModels.LoginViewModel");

        Assert.NotNull(loginViewModelType);

        var authApiClientType = assembly.GetType("DietPlanner.Mobile.Services.IAuthApiClient");
        var sessionStoreType = assembly.GetType("DietPlanner.Mobile.Services.ISessionStore");
        var navigatorType = assembly.GetType("DietPlanner.Mobile.Navigation.IAppNavigator");

        Assert.NotNull(authApiClientType);
        Assert.NotNull(sessionStoreType);
        Assert.NotNull(navigatorType);

        var authHandler = new AuthApiClientHandler();
        var sessionHandler = new SessionStoreHandler();
        var navigatorHandler = new NavigatorHandler();

        var authClient = TestProxy.Create(authApiClientType!, authHandler);
        var sessionStore = TestProxy.Create(sessionStoreType!, sessionHandler);
        var navigator = TestProxy.Create(navigatorType!, navigatorHandler);

        var viewModel = Activator.CreateInstance(loginViewModelType!, authClient, sessionStore, navigator);
        Assert.NotNull(viewModel);

        var command = loginViewModelType!.GetProperty("LoginWithGoogleCommand", BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(viewModel);

        Assert.NotNull(command);

        var executeAsync = command!.GetType().GetMethod("ExecuteAsync", new[] { typeof(object) });
        Assert.NotNull(executeAsync);

        await (Task)executeAsync!.Invoke(command, new object?[] { null })!;

        Assert.Equal("mobile-token", sessionHandler.StoredAccessToken);
        Assert.Equal("//home", navigatorHandler.LastRoute);
    }

    private sealed class AuthApiClientHandler : DispatchProxyHandler
    {
        public override object? Invoke(MethodInfo targetMethod, object?[]? args)
        {
            if (targetMethod.Name != "LoginWithGoogleAsync")
            {
                throw new NotSupportedException($"Unexpected auth client call: {targetMethod.Name}");
            }

            var resultType = targetMethod.ReturnType.GenericTypeArguments.Single();
            var session = Activator.CreateInstance(resultType, "mobile-token");
            var fromResult = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Task.FromResult));

            return fromResult.MakeGenericMethod(resultType).Invoke(null, new[] { session });
        }
    }

    private sealed class SessionStoreHandler : DispatchProxyHandler
    {
        public string? StoredAccessToken { get; private set; }

        public override object? Invoke(MethodInfo targetMethod, object?[]? args)
        {
            if (targetMethod.Name != "SetSession")
            {
                throw new NotSupportedException($"Unexpected session store call: {targetMethod.Name}");
            }

            var session = args![0];
            StoredAccessToken = (string?)session?.GetType().GetProperty("AccessToken")?.GetValue(session);
            return null;
        }
    }

    private sealed class NavigatorHandler : DispatchProxyHandler
    {
        public string? LastRoute { get; private set; }

        public override object? Invoke(MethodInfo targetMethod, object?[]? args)
        {
            if (targetMethod.Name != "GoToAsync")
            {
                throw new NotSupportedException($"Unexpected navigation call: {targetMethod.Name}");
            }

            LastRoute = (string?)args![0];
            return Task.CompletedTask;
        }
    }

    private abstract class DispatchProxyHandler
    {
        public abstract object? Invoke(MethodInfo targetMethod, object?[]? args);
    }

    private class TestProxy : DispatchProxy
    {
        private DispatchProxyHandler? _handler;

        public static object Create(Type interfaceType, DispatchProxyHandler handler)
        {
            var createMethod = typeof(DispatchProxy)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Create) && method.IsGenericMethodDefinition);

            var proxy = createMethod.MakeGenericMethod(interfaceType, typeof(TestProxy)).Invoke(null, null)!;
            ((TestProxy)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null || _handler is null)
            {
                throw new InvalidOperationException("Proxy invocation was not initialized correctly.");
            }

            return _handler.Invoke(targetMethod, args);
        }
    }
}
