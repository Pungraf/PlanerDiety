using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Infrastructure.Persistence;

public static class SqliteSchemaBootstrapper
{
    public const string BaselineMigrationId = "20260525183000_BaselineCurrentSchema";

    private const string EfProductVersion = "8.0.10";
    private const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";
    private const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public static async Task InitializeAsync(DietPlannerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var providerName = dbContext.Database.ProviderName
            ?? throw new InvalidOperationException("Database provider name is missing.");

        if (!string.Equals(providerName, SqliteProviderName, StringComparison.Ordinal))
        {
            await InitializeNonSqliteAsync(dbContext, providerName, cancellationToken);
            return;
        }

        await InitializeSqliteAsync(dbContext, cancellationToken);
    }

    private static async Task InitializeNonSqliteAsync(
        DietPlannerDbContext dbContext,
        string providerName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        bool hasMigrationHistory;
        bool hasApplicationTables;
        IReadOnlyCollection<string> appliedMigrations;

        try
        {
            hasMigrationHistory = await TableExistsAsync(connection, providerName, "__EFMigrationsHistory", cancellationToken);
            hasApplicationTables = await HasApplicationTablesAsync(connection, providerName, cancellationToken);
            appliedMigrations = hasMigrationHistory
                ? await ReadAppliedMigrationsAsync(connection, cancellationToken)
                : [];
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        if (!hasMigrationHistory)
        {
            if (!hasApplicationTables)
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            }
            else
            {
                if (shouldCloseConnection)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                try
                {
                    await UpgradeLegacyPostgresSchemaAsync(connection, providerName, cancellationToken);
                }
                finally
                {
                    if (shouldCloseConnection)
                    {
                        await connection.CloseAsync();
                    }
                }
            }

            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await StampAppliedMigrationsAsync(connection, providerName, dbContext.Database.GetMigrations(), cancellationToken);
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }

            return;
        }

        if (hasApplicationTables && !appliedMigrations.Contains(BaselineMigrationId, StringComparer.Ordinal))
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await UpgradeLegacyPostgresSchemaAsync(connection, providerName, cancellationToken);
                await StampAppliedMigrationsAsync(connection, providerName, dbContext.Database.GetMigrations(), cancellationToken);
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }

            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static async Task InitializeSqliteAsync(DietPlannerDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var hasMigrationHistory = await TableExistsAsync(connection, SqliteProviderName, "__EFMigrationsHistory", cancellationToken);
            var hasApplicationTables = await HasApplicationTablesAsync(connection, SqliteProviderName, cancellationToken);

            if (hasApplicationTables && !hasMigrationHistory)
            {
                await UpgradeLegacySqliteSchemaAsync(connection, cancellationToken);
                await StampBaselineMigrationAsync(connection, SqliteProviderName, cancellationToken);
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

    private static async Task<bool> HasApplicationTablesAsync(
        DbConnection connection,
        string providerName,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = string.Equals(providerName, SqliteProviderName, StringComparison.Ordinal)
            ? """
              SELECT COUNT(*)
              FROM sqlite_master
              WHERE type = 'table'
                AND name NOT LIKE 'sqlite_%'
                AND name <> '__EFMigrationsHistory';
              """
            : """
              SELECT COUNT(*)
              FROM information_schema.tables
              WHERE table_schema = current_schema()
                AND table_name <> '__EFMigrationsHistory';
              """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<bool> TableExistsAsync(
        DbConnection connection,
        string providerName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        var parameterName = string.Equals(providerName, SqliteProviderName, StringComparison.Ordinal)
            ? "$name"
            : "@name";
        command.CommandText = string.Equals(providerName, SqliteProviderName, StringComparison.Ordinal)
            ? $"""
               SELECT COUNT(*)
               FROM sqlite_master
               WHERE type = 'table' AND name = {parameterName};
               """
            : $"""
               SELECT COUNT(*)
               FROM information_schema.tables
               WHERE table_schema = current_schema()
                 AND table_name = {parameterName};
               """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task UpgradeLegacySqliteSchemaAsync(DbConnection connection, CancellationToken cancellationToken)
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

    private static async Task UpgradeLegacyPostgresSchemaAsync(
        DbConnection connection,
        string providerName,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(providerName, PostgresProviderName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Legacy schema upgrade is not implemented for provider '{providerName}'.");
        }

        var command = connection.CreateCommand();
        command.CommandText =
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = current_schema()
                      AND table_name = 'ShoppingListItems'
                ) THEN
                    ALTER TABLE "ShoppingListItems"
                    ADD COLUMN IF NOT EXISTS "IsChecked" boolean NOT NULL DEFAULT FALSE;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = current_schema()
                      AND table_name = 'Meals'
                ) THEN
                    ALTER TABLE "Meals"
                    ADD COLUMN IF NOT EXISTS "Description" text NOT NULL DEFAULT '';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = current_schema()
                      AND table_name = 'ShoppingLists'
                ) THEN
                    ALTER TABLE "ShoppingLists"
                    ADD COLUMN IF NOT EXISTS "Name" text NOT NULL DEFAULT 'Shopping list';

                    ALTER TABLE "ShoppingLists"
                    ADD COLUMN IF NOT EXISTS "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
                END IF;
            END $$;

            DROP INDEX IF EXISTS "IX_ShoppingLists_WeeklyPlanId";
            CREATE INDEX IF NOT EXISTS "IX_ShoppingLists_WeeklyPlanId" ON "ShoppingLists" ("WeeklyPlanId");
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyCollection<string>> ReadAppliedMigrationsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT "MigrationId"
            FROM "__EFMigrationsHistory"
            ORDER BY "MigrationId";
            """;

        var migrationIds = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            migrationIds.Add(reader.GetString(0));
        }

        return migrationIds;
    }

    private static Task StampBaselineMigrationAsync(
        DbConnection connection,
        string providerName,
        CancellationToken cancellationToken)
        => StampAppliedMigrationsAsync(connection, providerName, [BaselineMigrationId], cancellationToken);

    private static async Task StampAppliedMigrationsAsync(
        DbConnection connection,
        string providerName,
        IEnumerable<string> migrationIds,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();

        if (string.Equals(providerName, SqliteProviderName, StringComparison.Ordinal))
        {
            var statements = new List<string>
            {
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                    "ProductVersion" TEXT NOT NULL
                );
                """
            };

            foreach (var migrationId in migrationIds)
            {
                statements.Add(
                    $"""
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    SELECT '{migrationId}', '{EfProductVersion}'
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "__EFMigrationsHistory"
                        WHERE "MigrationId" = '{migrationId}'
                    );
                    """);
            }

            command.CommandText = string.Join(Environment.NewLine + Environment.NewLine, statements);
        }
        else if (string.Equals(providerName, PostgresProviderName, StringComparison.Ordinal))
        {
            var statements = new List<string>
            {
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """
            };

            foreach (var migrationId in migrationIds)
            {
                statements.Add(
                    $"""
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ('{migrationId}', '{EfProductVersion}')
                    ON CONFLICT ("MigrationId") DO NOTHING;
                    """);
            }

            command.CommandText = string.Join(Environment.NewLine + Environment.NewLine, statements);
        }
        else
        {
            throw new InvalidOperationException($"Migration history stamping is not implemented for provider '{providerName}'.");
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
