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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    private sealed record GoogleLoginRequest(string IdToken);

    private sealed record LoginResponse(string AccessToken);

    private sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public static async Task<ApiFactory> CreateAsync()
        {
            var factory = new ApiFactory();
            await factory._connection.OpenAsync();
            return factory;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DietPlannerDbContext>>();
                services.RemoveAll<DietPlannerDbContext>();
                services.RemoveAll<IApplicationDbContext>();
                services.RemoveAll<IGoogleTokenVerifier>();

                services.AddDbContext<DietPlannerDbContext>(options => options.UseSqlite(_connection));
                services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<DietPlannerDbContext>());
                services.AddSingleton<IGoogleTokenVerifier>(new FakeGoogleTokenVerifier());
            });
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class FakeGoogleTokenVerifier : IGoogleTokenVerifier
    {
        public Task<GoogleUserInfo> VerifyAsync(string idToken, CancellationToken cancellationToken)
        {
            idToken.Should().Be("valid-google-token");
            return Task.FromResult(new GoogleUserInfo("google-sub-123", "ada@example.com", "Ada Lovelace"));
        }
    }
}
