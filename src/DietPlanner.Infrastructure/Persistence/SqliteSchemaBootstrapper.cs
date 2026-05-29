using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Infrastructure.Persistence;

public static class SqliteSchemaBootstrapper
{
    public const string BaselineMigrationId = "20260525183000_BaselineCurrentSchema";

    private const string EfProductVersion = "8.0.10";

    public static async Task InitializeAsync(DietPlannerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (!string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var hasMigrationHistory = await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken);
            var hasApplicationTables = await HasApplicationTablesAsync(connection, cancellationToken);

            if (hasApplicationTables && !hasMigrationHistory)
            {
                await UpgradeLegacySchemaAsync(connection, cancellationToken);
                await StampBaselineMigrationAsync(connection, cancellationToken);
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static async Task<bool> HasApplicationTablesAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
              AND name <> '__EFMigrationsHistory';
            """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table' AND name = $name;
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task UpgradeLegacySchemaAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS "Ingredients" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Ingredients" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Unit" TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "Meals" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Meals" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "IsDessert" INTEGER NOT NULL,
                "Kcal" INTEGER NOT NULL,
                "Protein" INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS "MealIngredients" (
                "MealId" TEXT NOT NULL,
                "IngredientId" TEXT NOT NULL,
                "Quantity" TEXT NOT NULL,
                "Unit" TEXT NOT NULL,
                "ShoppingCategory" TEXT NOT NULL,
                CONSTRAINT "PK_MealIngredients" PRIMARY KEY ("MealId", "IngredientId"),
                CONSTRAINT "FK_MealIngredients_Ingredients_IngredientId" FOREIGN KEY ("IngredientId") REFERENCES "Ingredients" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_MealIngredients_Meals_MealId" FOREIGN KEY ("MealId") REFERENCES "Meals" ("Id") ON DELETE CASCADE
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task StampBaselineMigrationAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );

            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '{BaselineMigrationId}', '{EfProductVersion}'
            WHERE NOT EXISTS (
                SELECT 1
                FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '{BaselineMigrationId}'
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
