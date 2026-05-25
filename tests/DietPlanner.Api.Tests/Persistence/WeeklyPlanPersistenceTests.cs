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
        await using var db = fixture.CreateDbContext();
        var repository = new WeeklyPlanRepository(db);
        var plan = WeeklyPlanFactory.Create();

        await repository.AddAsync(plan, CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var saved = await db.WeeklyPlans
            .Include(x => x.Days)
            .ThenInclude(x => x.MealSlots)
            .SingleAsync();

        saved.Days.Should().HaveCount(7);
        saved.Days.SelectMany(day => day.MealSlots).Should().HaveCount(14);
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
            await db.Database.EnsureCreatedAsync();

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
}
