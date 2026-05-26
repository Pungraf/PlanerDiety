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
        factory.User = new User(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", "google-sub-123");
        await factory.SeedPlansAsync([CreateDraftPlan(factory.User.Id, new DateOnly(2026, 5, 25))]);
        return factory;
    }

    public static async Task<PlansApiFactory> WithActivePlanAsync(Func<Guid, WeeklyPlan> planFactory)
    {
        var factory = new PlansApiFactory();
        await factory._connection.OpenAsync();
        factory.User = new User(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", "google-sub-123");
        await factory.SeedPlansAsync([planFactory(factory.User.Id)]);
        return factory;
    }

    public static async Task<PlansApiFactory> WithPlansAsync(Func<Guid, IEnumerable<WeeklyPlan>> planFactory)
    {
        var factory = new PlansApiFactory();
        await factory._connection.OpenAsync();
        factory.User = new User(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", "google-sub-123");
        var plans = planFactory(factory.User.Id);
        await factory.SeedPlansAsync(plans);
        return factory;
    }

    public static async Task<PlansApiFactory> WithShoppingListAsync()
    {
        var factory = new PlansApiFactory();
        await factory._connection.OpenAsync();
        factory.User = new User(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", "google-sub-123");

        var plan = CreateActivePlan(factory.User.Id, new DateOnly(2026, 5, 25));
        var shoppingList = new ShoppingList(Guid.NewGuid(), plan.Id);
        shoppingList.ReplaceItems(
        [
            new ShoppingListItem(TestData.OatsShoppingListItemId, TestData.OatsIngredientId, 80m, "g"),
            new ShoppingListItem(TestData.ChickenShoppingListItemId, TestData.ChickenIngredientId, 140m, "g"),
            new ShoppingListItem(TestData.TomatoShoppingListItemId, TestData.TomatoIngredientId, 120m, "g")
        ]);

        await factory.SeedPlansAsync([plan], shoppingList);
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
        return await dbContext.WeeklyPlans
            .Include(plan => plan.Days)
            .ThenInclude(day => day.MealSlots)
            .SingleOrDefaultAsync(plan => plan.UserId == User.Id);
    }

    public async Task<IReadOnlyList<WeeklyPlan>> ReadPlansAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        return await dbContext.WeeklyPlans
            .Where(plan => plan.UserId == User.Id)
            .OrderBy(plan => plan.StartDate)
            .ToListAsync();
    }

    public async Task<ShoppingList?> ReadShoppingListAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        var plan = await dbContext.WeeklyPlans
            .Where(candidate => candidate.UserId == User.Id)
            .OrderByDescending(candidate => candidate.Status == WeeklyPlanStatus.Active)
            .ThenByDescending(candidate => candidate.StartDate)
            .Select(candidate => candidate.Id)
            .FirstOrDefaultAsync();

        return plan == Guid.Empty
            ? null
            : await dbContext.ShoppingLists
                .Include(list => list.Items)
                .SingleOrDefaultAsync(list => list.WeeklyPlanId == plan);
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

    private async Task SeedPlansAsync(IEnumerable<WeeklyPlan> plans, ShoppingList? shoppingList = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        await dbContext.Users.AddAsync(User);
        await dbContext.Ingredients.AddRangeAsync(TestData.CreateIngredients());
        await dbContext.Meals.AddRangeAsync(TestData.CreateMeals());
        await dbContext.WeeklyPlans.AddRangeAsync(plans);

        if (shoppingList is not null)
        {
            await dbContext.ShoppingLists.AddAsync(shoppingList);
        }

        await dbContext.SaveChangesAsync();
    }

    public static WeeklyPlan CreateDraftPlan(Guid userId, DateOnly startDate)
    {
        var plan = WeeklyPlan.CreateDraft(userId, startDate, DinnerMode.BreakfastStyle);
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

    public static WeeklyPlan CreateActivePlan(Guid userId, DateOnly startDate)
    {
        var plan = CreateDraftPlan(userId, startDate);
        plan.Activate();
        return plan;
    }

    public static WeeklyPlan CreateActivePlan(Guid userId, DateOnly startDate, Action<WeeklyPlan> configure)
    {
        var plan = CreateActivePlan(userId, startDate);
        configure(plan);
        return plan;
    }

    public static void AssignMeal(WeeklyPlan plan, DateOnly date, MealSlotType slotType, Guid mealId)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var slot = plan.Days
            .Single(day => day.Date == date)
            .MealSlots
            .Single(candidate => candidate.SlotType == slotType);

        slot.ReplaceMeal(mealId);
    }
}
