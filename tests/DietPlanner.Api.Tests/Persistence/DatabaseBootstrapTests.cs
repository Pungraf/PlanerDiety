using DietPlanner.Application.Abstractions;
using DietPlanner.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DietPlanner.Api.Tests.Persistence;

public class DatabaseBootstrapTests
{
    [Fact]
    public async Task Startup_ShouldUpgradeExistingDatabase_WhenTask8TablesAreMissing()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        try
        {
            await CreatePreTask8SchemaAsync(databasePath);

            await using (var app = new BootstrapApiFactory(databasePath))
            {
                using var client = app.CreateClient();

                var response = await client.GetAsync("/");
                response.EnsureSuccessStatusCode();

                await using var connection = new SqliteConnection($"Data Source={databasePath}");
                await connection.OpenAsync();

                var tableNames = await ReadTableNamesAsync(connection);
                var mealIngredientForeignKeys = await ReadForeignKeysAsync(connection, "MealIngredients");

                tableNames.Should().Contain(["Meals", "Ingredients", "MealIngredients", "__EFMigrationsHistory"]);
                mealIngredientForeignKeys.Should().ContainEquivalentOf(new ForeignKeyDefinition(
                    "IngredientId",
                    "Ingredients",
                    "Id"));
            }
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                try
                {
                    File.Delete(databasePath);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    private static async Task CreatePreTask8SchemaAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE "Users" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Email" TEXT NULL,
                "GoogleSubject" TEXT NULL
            );

            CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email") WHERE "Email" IS NOT NULL;
            CREATE UNIQUE INDEX "IX_Users_GoogleSubject" ON "Users" ("GoogleSubject") WHERE "GoogleSubject" IS NOT NULL;

            CREATE TABLE "WeeklyPlans" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_WeeklyPlans" PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "StartDate" TEXT NOT NULL,
                "DinnerMode" TEXT NOT NULL,
                "Status" TEXT NOT NULL
            );

            CREATE TABLE "DailyPlans" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_DailyPlans" PRIMARY KEY,
                "Date" TEXT NOT NULL,
                "WeeklyPlanId" TEXT NOT NULL,
                CONSTRAINT "FK_DailyPlans_WeeklyPlans_WeeklyPlanId" FOREIGN KEY ("WeeklyPlanId") REFERENCES "WeeklyPlans" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE "DailyMealSlots" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_DailyMealSlots" PRIMARY KEY,
                "SlotType" TEXT NOT NULL,
                "MealId" TEXT NULL,
                "DailyPlanId" TEXT NOT NULL,
                CONSTRAINT "FK_DailyMealSlots_DailyPlans_DailyPlanId" FOREIGN KEY ("DailyPlanId") REFERENCES "DailyPlans" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE "ShoppingLists" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ShoppingLists" PRIMARY KEY,
                "WeeklyPlanId" TEXT NOT NULL,
                CONSTRAINT "FK_ShoppingLists_WeeklyPlans_WeeklyPlanId" FOREIGN KEY ("WeeklyPlanId") REFERENCES "WeeklyPlans" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE "ShoppingListItems" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ShoppingListItems" PRIMARY KEY,
                "IngredientId" TEXT NOT NULL,
                "Quantity" TEXT NOT NULL,
                "Unit" TEXT NOT NULL,
                "ShoppingListId" TEXT NOT NULL,
                CONSTRAINT "FK_ShoppingListItems_ShoppingLists_ShoppingListId" FOREIGN KEY ("ShoppingListId") REFERENCES "ShoppingLists" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX "IX_DailyMealSlots_DailyPlanId" ON "DailyMealSlots" ("DailyPlanId");
            CREATE INDEX "IX_DailyPlans_WeeklyPlanId" ON "DailyPlans" ("WeeklyPlanId");
            CREATE UNIQUE INDEX "IX_ShoppingLists_WeeklyPlanId" ON "ShoppingLists" ("WeeklyPlanId");
            CREATE INDEX "IX_ShoppingListItems_ShoppingListId" ON "ShoppingListItems" ("ShoppingListId");
            """;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<IReadOnlyList<string>> ReadTableNamesAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<IReadOnlyList<ForeignKeyDefinition>> ReadForeignKeysAsync(
        SqliteConnection connection,
        string tableName)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list(\"{tableName}\");";

        var foreignKeys = new List<ForeignKeyDefinition>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            foreignKeys.Add(new ForeignKeyDefinition(
                reader.GetString(reader.GetOrdinal("from")),
                reader.GetString(reader.GetOrdinal("table")),
                reader.GetString(reader.GetOrdinal("to"))));
        }

        return foreignKeys;
    }

    private sealed record ForeignKeyDefinition(string FromColumn, string TargetTable, string TargetColumn);

    private sealed class BootstrapApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly string _databasePath;

        public BootstrapApiFactory(string databasePath)
        {
            _databasePath = databasePath;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DietPlanner"] = $"Data Source={_databasePath}",
                    ["Jwt:Key"] = "test-signing-key-with-minimum-length-123456"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DietPlannerDbContext>>();
                services.RemoveAll<DietPlannerDbContext>();
                services.RemoveAll<IApplicationDbContext>();

                services.AddDbContext<DietPlannerDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
                services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<DietPlannerDbContext>());
            });
        }
    }
}
