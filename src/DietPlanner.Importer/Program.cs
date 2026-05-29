using DietPlanner.Importer.Import;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Importer;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var options = ParseArguments(args);
        var importResult = await ImportAsync(options);

        var dbContextOptions = new DbContextOptionsBuilder<DietPlannerDbContext>();
        if (LooksLikePostgresConnectionString(options.ConnectionString))
        {
            dbContextOptions.UseNpgsql(options.ConnectionString);
        }
        else
        {
            dbContextOptions.UseSqlite(options.ConnectionString);
        }

        await using var dbContext = new DietPlannerDbContext(dbContextOptions.Options);
        await SqliteSchemaBootstrapper.InitializeAsync(dbContext);

        var writer = new ImportedMealCatalogWriter();
        var summary = await writer.WriteAsync(dbContext, importResult, CancellationToken.None);

        Console.WriteLine(
            $"Imported {summary.MealCount} meals, {summary.IngredientCount} ingredients and {summary.MealIngredientCount} meal ingredient rows.");
    }

    private static async Task<MealImportResult> ImportAsync(ImportArguments arguments)
    {
        if (arguments.CategoryCsvPath is null)
        {
            using var reader = File.OpenText(arguments.RecipeCsvPath);
            var importer = new MealImporter();
            return await importer.ImportAsync(reader, CancellationToken.None);
        }

        using var recipesReader = File.OpenText(arguments.RecipeCsvPath);
        using var categoriesReader = File.OpenText(arguments.CategoryCsvPath);
        var sourceImporter = new SourceRecipeImporter();
        return await sourceImporter.ImportAsync(recipesReader, categoriesReader, CancellationToken.None);
    }

    private static ImportArguments ParseArguments(string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            throw new ArgumentException(
                "Missing CSV path. Usage: DietPlanner.Importer <csv-path> [connection-string] or DietPlanner.Importer <recipes-csv-path> <categories-csv-path> [connection-string]");
        }

        var recipeCsvPath = args[0];
        if (!File.Exists(recipeCsvPath))
        {
            throw new FileNotFoundException("CSV file was not found.", recipeCsvPath);
        }

        string? categoryCsvPath = null;
        string? connectionString = null;

        if (args.Length > 1 && File.Exists(args[1]))
        {
            categoryCsvPath = args[1];
            connectionString = args.Length > 2
                ? args[2]
                : Environment.GetEnvironmentVariable("ConnectionStrings__DietPlanner");
        }
        else
        {
            connectionString = args.Length > 1
                ? args[1]
                : Environment.GetEnvironmentVariable("ConnectionStrings__DietPlanner");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Missing database connection string. Pass it as the second argument or set ConnectionStrings__DietPlanner.");
        }

        return new ImportArguments(recipeCsvPath, categoryCsvPath, connectionString);
    }

    private static bool LooksLikePostgresConnectionString(string connectionString)
        => connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase);

    private sealed record ImportArguments(string RecipeCsvPath, string? CategoryCsvPath, string ConnectionString);
}
