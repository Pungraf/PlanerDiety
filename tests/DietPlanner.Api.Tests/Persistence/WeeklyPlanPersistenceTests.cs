using System.Reflection;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Enums;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Tests.Persistence;

public class WeeklyPlanPersistenceTests
{
    [Fact]
    public async Task SavePlan_ShouldPersistNestedDaysAndSlots()
    {
        await using var fixture = await SqliteFixture.StartAsync();
        await using var writeDb = fixture.CreateDbContext();
        var repository = new WeeklyPlanRepository(writeDb);
        var plan = WeeklyPlanFactory.Create();

        await repository.AddAsync(plan, CancellationToken.None);
        await writeDb.SaveChangesAsync(CancellationToken.None);

        await using var readDb = fixture.CreateDbContext();
        var saved = await readDb.WeeklyPlans
            .Include(x => x.Days)
            .ThenInclude(x => x.MealSlots)
            .SingleAsync();

        saved.Days.Should().HaveCount(7);
        saved.Days.SelectMany(day => day.MealSlots).Should().HaveCount(14);
    }

    [Fact]
    public async Task SaveShoppingList_ShouldPersistItems_WhenReadFromFreshContext()
    {
        await using var fixture = await SqliteFixture.StartAsync();
        var weeklyPlan = WeeklyPlanFactory.Create();
        var shoppingList = ShoppingListFactory.Create(weeklyPlan.Id);

        await using (var writeDb = fixture.CreateDbContext())
        {
            var repository = new WeeklyPlanRepository(writeDb);
            await repository.AddAsync(weeklyPlan, CancellationToken.None);
            await writeDb.ShoppingLists.AddAsync(shoppingList, CancellationToken.None);
            await writeDb.SaveChangesAsync(CancellationToken.None);
        }

        await using var readDb = fixture.CreateDbContext();
        var saved = await readDb.ShoppingLists
            .Include(x => x.Items)
            .SingleAsync();

        saved.WeeklyPlanId.Should().Be(weeklyPlan.Id);
        saved.Items.Should().HaveCount(2);
        saved.Items.Select(x => x.Unit).Should().BeEquivalentTo(["g", "ml"]);
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SqliteFixture(SqliteConnection connection)
        {
            _connection = connection;
        }

        public static async Task<SqliteFixture> StartAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            await using var db = CreateDbContext(connection);
            await SqliteSchemaBootstrapper.InitializeAsync(db);

            return new SqliteFixture(connection);
        }

        public DietPlannerDbContext CreateDbContext()
        {
            return CreateDbContext(_connection);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        private static DietPlannerDbContext CreateDbContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<DietPlannerDbContext>()
                .UseSqlite(connection)
                .Options;

            return new DietPlannerDbContext(options);
        }
    }

    private static class WeeklyPlanFactory
    {
        public static WeeklyPlan Create()
        {
            var plan = WeeklyPlan.CreateDraft(Guid.NewGuid(), new DateOnly(2026, 5, 25), DinnerMode.BreakfastStyle);
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

    private static class ShoppingListFactory
    {
        public static ShoppingList Create(Guid weeklyPlanId)
        {
            var shoppingList = new ShoppingList(Guid.NewGuid(), weeklyPlanId);
            var itemsField = typeof(ShoppingList).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var items = (List<ShoppingListItem>)itemsField.GetValue(shoppingList)!;

            items.Add(new ShoppingListItem(Guid.NewGuid(), Guid.NewGuid(), 200m, "g"));
            items.Add(new ShoppingListItem(Guid.NewGuid(), Guid.NewGuid(), 500m, "ml"));

            return shoppingList;
        }
    }
}
