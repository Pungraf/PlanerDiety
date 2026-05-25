using System.Net;
using System.Net.Http.Json;
using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Auth;
using DietPlanner.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DietPlanner.Domain.Entities;
using DietPlanner.Infrastructure.Auth;

namespace DietPlanner.Api.Tests.Auth;

public class GoogleLoginTests
{
    [Fact]
    public async Task GoogleLogin_ShouldCreateUserAndReturnSessionToken()
    {
        await using var app = await ApiFactory.CreateAsync();
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest("valid-google-token"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        payload.Should().NotBeNull();
        payload!.AccessToken.Should().NotBeNullOrWhiteSpace();

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        var user = await dbContext.Users.SingleAsync();
        user.Email.Should().Be("ada@example.com");
        user.GoogleSubject.Should().Be("google-sub-123");
        user.Name.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task GoogleLogin_ShouldRejectEmailLink_WhenGoogleEmailIsNotVerified()
    {
        await using var app = await ApiFactory.CreateAsync(new GoogleUserInfo("new-google-sub", "ada@example.com", "Ada Impostor", false));
        await app.SeedUserAsync(new User(Guid.NewGuid(), "Existing Ada", "ada@example.com", null));
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest("valid-google-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        var user = await dbContext.Users.SingleAsync();
        user.Name.Should().Be("Existing Ada");
        user.GoogleSubject.Should().BeNull();
    }

    [Fact]
    public async Task GoogleLogin_ShouldRejectLogin_WhenGoogleEmailIsNotVerified_AndNoGoogleSubjectMatchExists()
    {
        await using var app = await ApiFactory.CreateAsync(new GoogleUserInfo("new-google-sub", "new@example.com", "New User", false));
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest("valid-google-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        (await dbContext.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void JwtSessionTokenService_ShouldThrow_WhenJwtKeyIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var act = () => new JwtSessionTokenService(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key*");
    }

    private sealed record GoogleLoginRequest(string IdToken);

    private sealed record LoginResponse(string AccessToken);

    private sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        private readonly GoogleUserInfo _googleUserInfo;

        private ApiFactory(GoogleUserInfo googleUserInfo)
        {
            _googleUserInfo = googleUserInfo;
        }

        public static async Task<ApiFactory> CreateAsync(GoogleUserInfo? googleUserInfo = null)
        {
            var factory = new ApiFactory(googleUserInfo ?? new GoogleUserInfo("google-sub-123", "ada@example.com", "Ada Lovelace", true));
            await factory._connection.OpenAsync();
            return factory;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "test-signing-key-with-minimum-length-123456"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DietPlannerDbContext>>();
                services.RemoveAll<DietPlannerDbContext>();
                services.RemoveAll<IApplicationDbContext>();
                services.RemoveAll<IGoogleTokenVerifier>();

                services.AddDbContext<DietPlannerDbContext>(options => options.UseSqlite(_connection));
                services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<DietPlannerDbContext>());
                services.AddSingleton<IGoogleTokenVerifier>(new FakeGoogleTokenVerifier(_googleUserInfo));
            });
        }

        public async Task SeedUserAsync(User user)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        private readonly GoogleUserInfo _googleUserInfo;

        public FakeGoogleTokenVerifier(GoogleUserInfo googleUserInfo)
        {
            _googleUserInfo = googleUserInfo;
        }

        public Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken)
        {
            idToken.Should().Be("valid-google-token");
            return Task.FromResult(_googleUserInfo);
        }
    }
}
