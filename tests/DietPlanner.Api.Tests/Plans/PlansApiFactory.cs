using System.Net.Http.Headers;
using System.Reflection;
using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Auth;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using DietPlanner.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DietPlanner.Api.Tests.Plans;

internal sealed class PlansApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    private PlansApiFactory()
    {
    }

    public User User { get; private set; } = null!;

    public static async Task<PlansApiFactory> WithDraftPlanAsync()
    {
        var factory = new PlansApiFactory();
        await factory._connection.OpenAsync();
        await factory.SeedDraftPlanAsync();
        return factory;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();

        await using var scope = Services.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ISessionTokenService>();
        var token = tokenService.CreateToken(User);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<WeeklyPlan?> ReadCurrentPlanAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        return await dbContext.WeeklyPlans.SingleOrDefaultAsync(plan => plan.UserId == User.Id);
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

            services.AddDbContext<DietPlannerDbContext>(options => options.UseSqlite(_connection));
            services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<DietPlannerDbContext>());
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task SeedDraftPlanAsync()
    {
        User = new User(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", "google-sub-123");
        var plan = CreateDraftPlan(User.Id);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        await dbContext.Users.AddAsync(User);
        await dbContext.WeeklyPlans.AddAsync(plan);
        await dbContext.SaveChangesAsync();
    }

    private static WeeklyPlan CreateDraftPlan(Guid userId)
    {
        var plan = WeeklyPlan.CreateDraft(userId, new DateOnly(2026, 5, 25), DinnerMode.BreakfastStyle);
        var addDay = typeof(WeeklyPlan).GetMethod("AddDay", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var addSlot = typeof(DailyPlan).GetMethod("AddSlot", BindingFlags.Instance | BindingFlags.NonPublic)!;

        for (var offset = 0; offset < 7; offset++)
        {
            var day = new DailyPlan(Guid.NewGuid(), plan.StartDate.AddDays(offset));
            addSlot.Invoke(day, [new DailyMealSlot(Guid.NewGuid(), MealSlotType.Breakfast)]);
            addSlot.Invoke(day, [new DailyMealSlot(Guid.NewGuid(), MealSlotType.Dinner)]);
            addDay.Invoke(plan, [day]);
        }

        return plan;
    }
}
